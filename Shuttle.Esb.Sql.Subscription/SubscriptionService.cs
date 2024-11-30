using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription;

public class SubscriptionService : ISubscriptionService, IDisposable, IPipelineObserver<OnStarted>
{
    private readonly IQueryFactory _queryFactory;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly IDatabaseContextFactory _databaseContextFactory;
    private readonly IPipelineFactory _pipelineFactory;
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly SqlSubscriptionOptions _sqlSubscriptionOptions;
    private readonly MemoryCache _subscribersCache = new("Shuttle.Esb.Sql.Subscription:Subscribers");

    public SubscriptionService(IOptions<ServiceBusOptions> serviceBusOptions, IOptions<SqlSubscriptionOptions> sqlSubscriptionOptions, IPipelineFactory pipelineFactory, IDatabaseContextFactory databaseContextFactory, IQueryFactory queryFactory)
    {
        _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
        _sqlSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);
        _pipelineFactory = Guard.AgainstNull(pipelineFactory);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);
        _queryFactory = Guard.AgainstNull(queryFactory);

        pipelineFactory.PipelineCreated += PipelineCreated;
    }

    public void Dispose()
    {
        _pipelineFactory.PipelineCreated -= PipelineCreated;
    }

    public async Task ExecuteAsync(IPipelineContext<OnStarted> pipelineContext)
    {
        if (string.IsNullOrWhiteSpace(_serviceBusOptions.Inbox.WorkQueueUri))
        {
            throw new InvalidOperationException(Esb.Resources.SubscribeWithNoInboxException);
        }

        var messageTypes = _serviceBusOptions.Subscription.MessageTypes;

        if (!messageTypes.Any() ||
            _serviceBusOptions.Subscription.SubscribeType == SubscribeType.Ignore)
        {
            return;
        }

        var missingMessageTypes = new List<string>();

        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_sqlSubscriptionOptions.ConnectionStringName))
        {
            await databaseContext.ExecuteAsync(_queryFactory.Create());
        }

        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_sqlSubscriptionOptions.ConnectionStringName))
        {
            foreach (var messageType in messageTypes)
            {
                switch (_serviceBusOptions.Subscription.SubscribeType)
                {
                    case SubscribeType.Normal:
                    {
                        await databaseContext.ExecuteAsync(_queryFactory.Subscribe(messageType)).ConfigureAwait(false);

                        break;
                    }
                    case SubscribeType.Ensure:
                    {
                        var count = await databaseContext.GetScalarAsync<int>(_queryFactory.Contains(messageType)).ConfigureAwait(false);

                        if (count == 0)
                        {
                            missingMessageTypes.Add(messageType);
                        }

                        break;
                    }
                }
            }
        }

        if (!missingMessageTypes.Any())
        {
            return;
        }

        throw new ApplicationException(string.Format(Resources.MissingSubscriptionException, string.Join(",", missingMessageTypes)));
    }

    public async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType)
    {
        Guard.AgainstNullOrEmptyString(messageType);

        await Lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (!_subscribersCache.Contains(messageType))
            {
                IEnumerable<DataRow> rows;

                using (new DatabaseContextScope())
                await using (var databaseContext = _databaseContextFactory.Create(_sqlSubscriptionOptions.ConnectionStringName))
                {
                    rows = await databaseContext.GetRowsAsync(_queryFactory.GetSubscribedUris(messageType));
                }

                _subscribersCache.Set(messageType, (from DataRow row in rows select Columns.InboxWorkQueueUri.Value(row)).ToList(), DateTimeOffset.Now.Add(_sqlSubscriptionOptions.CacheTimeout));
            }
        }
        finally
        {
            Lock.Release();
        }

        return _subscribersCache.Get(messageType) as IEnumerable<string> ?? [];
    }

    private void PipelineCreated(object? sender, PipelineEventArgs args)
    {
        Guard.AgainstNull(args);

        if (args.Pipeline.GetType() != typeof(StartupPipeline))
        {
            return;
        }

        args.Pipeline.AddObserver(this);
    }
}