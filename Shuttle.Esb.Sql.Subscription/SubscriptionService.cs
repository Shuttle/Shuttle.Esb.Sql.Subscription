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
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly IDatabaseContextFactory _databaseContextFactory;
    private readonly IPipelineFactory _pipelineFactory;
    private readonly IScriptProvider _scriptProvider;
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly MemoryCache _subscribersCache = new("Shuttle.Esb.Sql.Subscription:Subscribers");

    public SubscriptionService(IOptionsMonitor<ConnectionStringOptions> connectionStringOptions, IOptions<ServiceBusOptions> serviceBusOptions, IPipelineFactory pipelineFactory, IScriptProvider scriptProvider, IDatabaseContextFactory databaseContextFactory)
    {
        Guard.AgainstNull(connectionStringOptions);

        _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
        _pipelineFactory = Guard.AgainstNull(pipelineFactory);
        _scriptProvider = Guard.AgainstNull(scriptProvider);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);

        pipelineFactory.PipelineCreated += PipelineCreated;

        using (var databaseContext = _databaseContextFactory.Create(_serviceBusOptions.Subscription.ConnectionStringName))
        {
            if (databaseContext.GetScalarAsync<int>(new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceExists))).GetAwaiter().GetResult() != 1)
            {
                try
                {
                    databaseContext.ExecuteAsync(new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceCreate))).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Equals("There is already an object named 'SubscriberMessageType' in the database.", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new DataException(Resources.SubscriptionManagerCreateException, ex);
                    }
                }
            }
        }
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

        await using (var databaseContext = _databaseContextFactory.Create(_serviceBusOptions.Subscription.ConnectionStringName))
        {
            foreach (var messageType in messageTypes)
            {
                switch (_serviceBusOptions.Subscription.SubscribeType)
                {
                    case SubscribeType.Normal:
                    {
                        var query = new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceSubscribe))
                            .AddParameter(Columns.InboxWorkQueueUri, _serviceBusOptions.Inbox.WorkQueueUri)
                            .AddParameter(Columns.MessageType, messageType);

                        await databaseContext.ExecuteAsync(query).ConfigureAwait(false);

                        break;
                    }
                    case SubscribeType.Ensure:
                    {
                        var query = new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceContains))
                            .AddParameter(Columns.InboxWorkQueueUri, _serviceBusOptions.Inbox.WorkQueueUri)
                            .AddParameter(Columns.MessageType, messageType);

                        var count = await databaseContext.GetScalarAsync<int>(query).ConfigureAwait(false);

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

                await using (var databaseContext = _databaseContextFactory.Create(_serviceBusOptions.Subscription.ConnectionStringName))
                {
                    var query =
                        new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceInboxWorkQueueUris))
                            .AddParameter(Columns.MessageType, messageType);

                    rows = await databaseContext.GetRowsAsync(query);
                }

                _subscribersCache.Set(messageType, (from DataRow row in rows select Columns.InboxWorkQueueUri.Value(row)).ToList(), DateTimeOffset.Now.Add(_serviceBusOptions.Subscription.CacheTimeout));
            }
        }
        finally
        {
            Lock.Release();
        }

        return _subscribersCache.Get(messageType) as IEnumerable<string> ?? Enumerable.Empty<string>();
    }

    private void PipelineCreated(object? sender, PipelineEventArgs args)
    {
        Guard.AgainstNull(args);

        if (args.Pipeline.GetType() != typeof(StartupPipeline))
        {
            return;
        }

        args.Pipeline.RegisterObserver(this);
    }
}