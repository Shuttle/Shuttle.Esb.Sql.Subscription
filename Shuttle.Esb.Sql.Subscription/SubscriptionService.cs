using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription
{
    public class SubscriptionService : ISubscriptionService, IDisposable, IPipelineObserver<OnStarted>
    {
        private static readonly object Padlock = new object();
        private readonly IDatabaseContextFactory _databaseContextFactory;

        private readonly IDatabaseGateway _databaseGateway;

        private readonly List<string> _deferredSubscriptions = new List<string>();

        private readonly IScriptProvider _scriptProvider;

        private readonly IOptionsMonitor<ConnectionStringOptions> _connectionStringOptions;
        private readonly IServiceBusConfiguration _serviceBusConfiguration;
        private readonly IPipelineFactory _pipelineFactory;

        private readonly Dictionary<string, List<string>> _subscribers = new Dictionary<string, List<string>>();
        private readonly string _subscriptionConnectionString;
        private readonly string _subscriptionProviderName;

        private bool _deferSubscriptions = true;
        private readonly SubscriptionOptions _subscriptionOptions;

        public SubscriptionService(IOptionsMonitor<ConnectionStringOptions> connectionStringOptions, IOptions<SubscriptionOptions> subscriptionOptions, IServiceBusConfiguration serviceBusConfiguration, IPipelineFactory pipelineFactory, IScriptProvider scriptProvider, IDatabaseContextFactory databaseContextFactory, IDatabaseGateway databaseGateway)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));
            Guard.AgainstNull(subscriptionOptions, nameof(subscriptionOptions));
            Guard.AgainstNull(subscriptionOptions.Value, nameof(subscriptionOptions.Value));
            Guard.AgainstNull(serviceBusConfiguration, nameof(serviceBusConfiguration));
            Guard.AgainstNull(pipelineFactory, nameof(pipelineFactory));
            Guard.AgainstNull(scriptProvider, nameof(scriptProvider));
            Guard.AgainstNull(databaseContextFactory, nameof(databaseContextFactory));
            Guard.AgainstNull(databaseGateway, nameof(databaseGateway));

            _connectionStringOptions = connectionStringOptions;
            _serviceBusConfiguration = serviceBusConfiguration;
            _pipelineFactory = pipelineFactory;
            _scriptProvider = scriptProvider;
            _databaseContextFactory = databaseContextFactory;
            _databaseGateway = databaseGateway;

            pipelineFactory.PipelineCreated += PipelineCreated;

            _subscriptionOptions = subscriptionOptions.Value;

            var connectionStringName = subscriptionOptions.Value.ConnectionStringName;
            var connectionString = connectionStringOptions.Get(connectionStringName);

            if (connectionString == null)
            {
                throw new InvalidOperationException(string.Format(Shuttle.Core.Data.Resources.ConnectionSettingsMissing, connectionStringName));
            }

            _subscriptionProviderName = connectionString.ProviderName;
            _subscriptionConnectionString = connectionString.ConnectionString;

            using (_databaseContextFactory.Create(_subscriptionProviderName, _subscriptionConnectionString))
            {
                if (_databaseGateway.GetScalar<int>(
                        RawQuery.Create(
                            _scriptProvider.Get(
                                Script.SubscriptionManagerExists))) != 1)
                {
                    try
                    {
                        _databaseGateway.Execute(RawQuery.Create(
                            _scriptProvider.Get(
                                Script.SubscriptionManagerCreate)));
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

        protected bool HasDeferredSubscriptions => _deferredSubscriptions.Count > 0;

        public void Subscribe(IEnumerable<string> messageTypeFullNames)
        {
            Subscribe(_subscriptionProviderName, _subscriptionConnectionString, messageTypeFullNames);
        }

        public void Subscribe(string connectionStringName, IEnumerable<string> messageTypeFullNames)
        {
            var connectionString = _connectionStringOptions.Get(connectionStringName);

            if (connectionString == null)
            {
                throw new InvalidOperationException(string.Format(Shuttle.Core.Data.Resources.ConnectionSettingsMissing, connectionStringName));
            }

            Subscribe(connectionString.ProviderName, connectionString.ConnectionString, messageTypeFullNames);
        }

        public void Subscribe(string providerName, string connectionString, IEnumerable<string> messageTypeFullNames)
        {
            Guard.AgainstNull(messageTypeFullNames, "messageTypeFullNames");

            if (_deferSubscriptions)
            {
                _deferredSubscriptions.AddRange(messageTypeFullNames);

                return;
            }

            if (_serviceBusConfiguration.IsWorker() || _subscriptionOptions.SubscribeType == SubscribeType.Ignore)
            {
                return;
            }

            if (!_serviceBusConfiguration.HasInbox()
                ||
                _serviceBusConfiguration.Inbox.WorkQueue == null)
            {
                throw new InvalidOperationException(Esb.Resources.SubscribeWithNoInboxException);
            }

            var missingMessageTypes = new List<string>();

            using (_databaseContextFactory.Create(providerName, connectionString))
            {
                foreach (var messageType in messageTypeFullNames)
                {
                    if (_subscriptionOptions.SubscribeType == SubscribeType.Normal)
                    {
                        _databaseGateway.Execute(
                            RawQuery.Create(
                                    _scriptProvider.Get(Script.SubscriptionManagerSubscribe))
                                .AddParameterValue(Columns.InboxWorkQueueUri,
                                    _serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString())
                                .AddParameterValue(Columns.MessageType, messageType));
                    }
                    else // Ensure
                    {
                        if (_databaseGateway.GetScalar<int>(
                                RawQuery.Create(
                                        _scriptProvider.Get(Script.SubscriptionManagerContains))
                                    .AddParameterValue(Columns.InboxWorkQueueUri,
                                        _serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString())
                                    .AddParameterValue(Columns.MessageType, messageType)) == 0)
                        {
                            missingMessageTypes.Add(messageType);
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

        public IEnumerable<string> GetSubscribedUris(object message)
        {
            Guard.AgainstNull(message, "message");

            var messageType = message.GetType().FullName ?? string.Empty;

            if (!_subscribers.ContainsKey(messageType))
            {
                lock (Padlock)
                {
                    if (!_subscribers.ContainsKey(messageType))
                    {
                        DataTable table;

                        using (_databaseContextFactory.Create(_subscriptionProviderName, _subscriptionConnectionString))
                        {
                            table = _databaseGateway.GetDataTable(
                                RawQuery.Create(
                                        _scriptProvider.Get(
                                            Script.SubscriptionManagerInboxWorkQueueUris))
                                    .AddParameterValue(Columns.MessageType, messageType));
                        }

                        _subscribers.Add(messageType, (from DataRow row in table.Rows
                                select Columns.InboxWorkQueueUri.MapFrom(row))
                            .ToList());
                    }
                }
            }

            return _subscribers[messageType];
        }

        public void Dispose()
        {
            _pipelineFactory.PipelineCreated -= PipelineCreated;
        }

        public void Execute(OnStarted pipelineEvent)
        {
            Guard.AgainstNull(pipelineEvent, nameof(pipelineEvent));

            _deferSubscriptions = false;

            if (HasDeferredSubscriptions)
            {
                Subscribe(_deferredSubscriptions);
            }
        }
    }
}