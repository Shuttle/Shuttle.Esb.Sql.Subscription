using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly IDatabaseContextFactory _databaseContextFactory;
    private readonly IQueryFactory _queryFactory;
    private readonly SqlSubscriptionOptions _sqlSubscriptionOptions;
    private readonly MemoryCache _subscribersCache = new("Shuttle.Esb.Sql.Subscription:Subscribers");

    public SubscriptionService(IOptions<SqlSubscriptionOptions> sqlSubscriptionOptions, IDatabaseContextFactory databaseContextFactory, IQueryFactory queryFactory)
    {
        _sqlSubscriptionOptions = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);
        _queryFactory = Guard.AgainstNull(queryFactory);
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
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
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
}