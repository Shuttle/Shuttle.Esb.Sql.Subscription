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

namespace Shuttle.Esb.Sql.Subscription
{
    public class SubscriptionService : ISubscriptionService, IDisposable, IPipelineObserver<OnStarted>
    {
        private static readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);

        private readonly IDatabaseContextFactory _databaseContextFactory;
        private readonly IDatabaseGateway _databaseGateway;
        private readonly IPipelineFactory _pipelineFactory;
        private readonly IScriptProvider _scriptProvider;
        private readonly ServiceBusOptions _serviceBusOptions;
        private readonly MemoryCache _subscribersCache = new MemoryCache("Shuttle.Esb.Sql.Subscription:Subscribers");

        public SubscriptionService(IOptionsMonitor<ConnectionStringOptions> connectionStringOptions, IOptions<ServiceBusOptions> serviceBusOptions, IPipelineFactory pipelineFactory, IScriptProvider scriptProvider, IDatabaseContextFactory databaseContextFactory, IDatabaseGateway databaseGateway)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));
            Guard.AgainstNull(serviceBusOptions, nameof(serviceBusOptions));
            Guard.AgainstNull(serviceBusOptions.Value, nameof(serviceBusOptions.Value));
            
            _serviceBusOptions = serviceBusOptions.Value;
            _pipelineFactory = Guard.AgainstNull(pipelineFactory, nameof(pipelineFactory));
            _scriptProvider = Guard.AgainstNull(scriptProvider, nameof(scriptProvider));
            _databaseContextFactory = Guard.AgainstNull(databaseContextFactory, nameof(databaseContextFactory));
            _databaseGateway = Guard.AgainstNull(databaseGateway, nameof(databaseGateway));

            pipelineFactory.PipelineCreated += PipelineCreated;

            using (_databaseContextFactory.Create(_serviceBusOptions.Subscription.ConnectionStringName))
            {
                if (_databaseGateway.GetScalar<int>(
                        new Query(
                            _scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceExists))) != 1)
                {
                    try
                    {
                        _databaseGateway.Execute(new Query(
                            _scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceCreate)));
                    }
                    catch (Exception ex)
                    {
                        if (
                            !ex.Message.Equals(
                                "There is already an object named 'SubscriberMessageType' in the database.",
                                StringComparison.OrdinalIgnoreCase))
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

        public void Execute(OnStarted pipelineEvent)
        {
            ExecuteAsync(pipelineEvent, true).GetAwaiter().GetResult();
        }

        public async Task ExecuteAsync(OnStarted pipelineEvent)
        {
            await ExecuteAsync(pipelineEvent, false).ConfigureAwait(false);
        }

        public IEnumerable<string> GetSubscribedUris(string messageType)
        {
            return GetSubscribedUrisAsync(messageType, true).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType)
        {
            return await GetSubscribedUrisAsync(messageType, false).ConfigureAwait(false);
        }

        private async Task ExecuteAsync(OnStarted pipelineEvent, bool sync)
        {
            Guard.AgainstNull(pipelineEvent, nameof(pipelineEvent));

            if (!_serviceBusOptions.HasInbox())
            {
                throw new InvalidOperationException(Esb.Resources.SubscribeWithNoInboxException);
            }

            var messageTypes = _serviceBusOptions.Subscription?.MessageTypes ?? Enumerable.Empty<string>();

            if (!messageTypes.Any() ||
                _serviceBusOptions.Subscription.SubscribeType == SubscribeType.Ignore)
            {
                return;
            }

            var missingMessageTypes = new List<string>();

            await using (_databaseContextFactory.Create(_serviceBusOptions.Subscription.ConnectionStringName))
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

                                if (sync)
                                {
                                    _databaseGateway.Execute(query);
                                }
                                else
                                {
                                    await _databaseGateway.ExecuteAsync(query).ConfigureAwait(false);
                                }

                                break;
                            }
                        case SubscribeType.Ensure:
                            {
                                var query = new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceContains))
                                    .AddParameter(Columns.InboxWorkQueueUri, _serviceBusOptions.Inbox.WorkQueueUri)
                                    .AddParameter(Columns.MessageType, messageType);

                                var count = sync
                                    ? _databaseGateway.GetScalar<int>(query)
                                    : await _databaseGateway.GetScalarAsync<int>(query).ConfigureAwait(false);

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

            throw new ApplicationException(string.Format(Resources.MissingSubscriptionException,
                string.Join(",", missingMessageTypes)));
        }

        private async Task<IEnumerable<string>> GetSubscribedUrisAsync(string messageType, bool sync)
        {
            Guard.AgainstNullOrEmptyString(messageType, nameof(messageType));

            await Lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (!_subscribersCache.Contains(messageType))
                {
                    DataTable table;

                    using (_databaseContextFactory.Create(_serviceBusOptions.Subscription.ConnectionStringName))
                    {
                        var query =
                            new Query(_scriptProvider.Get(_serviceBusOptions.Subscription.ConnectionStringName, Script.SubscriptionServiceInboxWorkQueueUris))
                                .AddParameter(Columns.MessageType, messageType);

                        table = sync
                            ? _databaseGateway.GetDataTable(query)
                            : await _databaseGateway.GetDataTableAsync(query);
                    }

                    _subscribersCache.Set(messageType, (from DataRow row in table.Rows select Columns.InboxWorkQueueUri.Value(row)).ToList(), DateTimeOffset.Now.Add(_serviceBusOptions.Subscription.CacheTimeout));
                }
            }
            finally
            {
                Lock.Release();
            }

            return (IEnumerable<string>)_subscribersCache.Get(messageType);
        }

        private void PipelineCreated(object sender, PipelineEventArgs e)
        {
            Guard.AgainstNull(sender, nameof(sender));
            Guard.AgainstNull(e, nameof(e));

            if (e.Pipeline.GetType() != typeof(StartupPipeline))
            {
                return;
            }

            e.Pipeline.RegisterObserver(this);
        }
    }
}