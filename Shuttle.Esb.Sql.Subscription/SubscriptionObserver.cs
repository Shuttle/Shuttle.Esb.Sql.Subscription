using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription;

public class SubscriptionObserver : IPipelineObserver<OnStarted>
{
    private readonly IDatabaseContextFactory _databaseContextFactory;
    private readonly IQueryFactory _queryFactory;
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly SqlSubscriptionOptions _sqlSubscriptionOptions;

    public SubscriptionObserver(IOptions<ServiceBusOptions> serviceBusOptions, IOptions<SqlSubscriptionOptions> sqlSubscriptionOptions, IDatabaseContextFactory databaseContextFactory, IQueryFactory queryFactory)
    {
        _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
        _sqlSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);
        _queryFactory = Guard.AgainstNull(queryFactory);
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
}