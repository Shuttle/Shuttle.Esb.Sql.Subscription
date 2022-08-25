using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.Caching;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Logging;

namespace Shuttle.Esb.Sql.Subscription
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private static readonly object Padlock = new object();
        private readonly ISubscriptionConfiguration _configuration;
        private readonly IDatabaseContextFactory _databaseContextFactory;

        private readonly IDatabaseGateway _databaseGateway;

        private readonly List<string> _deferredSubscriptions = new List<string>();

        private readonly ILog _log;
        private readonly IScriptProvider _scriptProvider;

        private readonly IServiceBusConfiguration _serviceBusConfiguration;

        private readonly MemoryCache _subscribersCache = new MemoryCache("subscribers");
        private readonly string _subscriptionConnectionString;
        private readonly string _subscriptionProviderName;

        private bool _deferSubscriptions = true;

        public SubscriptionManager(IServiceBusEvents events, IServiceBusConfiguration serviceBusConfiguration,
            ISubscriptionConfiguration configuration, IScriptProvider scriptProvider,
            IDatabaseContextFactory databaseContextFactory, IDatabaseGateway databaseGateway)
        {
            Guard.AgainstNull(events, "events");
            Guard.AgainstNull(serviceBusConfiguration, "serviceBusConfiguration");
            Guard.AgainstNull(configuration, "configuration");
            Guard.AgainstNull(scriptProvider, "scriptProvider");
            Guard.AgainstNull(databaseContextFactory, "databaseContextFactory");
            Guard.AgainstNull(databaseGateway, "databaseGateway");

            _log = Log.For(this);

            _serviceBusConfiguration = serviceBusConfiguration;
            _configuration = configuration;
            _scriptProvider = scriptProvider;
            _databaseContextFactory = databaseContextFactory;
            _databaseGateway = databaseGateway;

            events.Started += ServiceBus_Started;

            _subscriptionProviderName = configuration.ProviderName;

            if (string.IsNullOrEmpty(_subscriptionProviderName))
            {
                throw new ConfigurationErrorsException(string.Format(Resources.ProviderNameEmpty,
                    "SubscriptionManager"));
            }

            _subscriptionConnectionString = configuration.ConnectionString;

            if (string.IsNullOrEmpty(_subscriptionConnectionString))
            {
                throw new ConfigurationErrorsException(string.Format(Resources.ConnectionStringEmpty,
                    "SubscriptionManager"));
            }

            using (_databaseContextFactory.Create(_subscriptionProviderName, _subscriptionConnectionString))
            {
                if (_databaseGateway.GetScalarUsing<int>(
                        RawQuery.Create(
                            _scriptProvider.Get(
                                Script.SubscriptionManagerExists))) != 1)
                {
                    try
                    {
                        _databaseGateway.ExecuteUsing(RawQuery.Create(
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

        protected bool HasDeferredSubscriptions => _deferredSubscriptions.Count > 0;

        public void Subscribe(IEnumerable<string> messageTypeFullNames)
        {
            Subscribe(_subscriptionProviderName, _subscriptionConnectionString, messageTypeFullNames);
        }

        public void Subscribe(string connectionStringName, IEnumerable<string> messageTypeFullNames)
        {
            var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (settings == null)
            {
                throw new InvalidOperationException(string.Format(Resources.ConnectionStringMissing, connectionStringName));
            }

            Subscribe(settings.ProviderName, settings.ConnectionString, messageTypeFullNames);
        }

        public void Subscribe(string providerName, string connectionString, IEnumerable<string> messageTypeFullNames)
        {
            Guard.AgainstNull(messageTypeFullNames, "messageTypeFullNames");

            if (_deferSubscriptions)
            {
                _deferredSubscriptions.AddRange(messageTypeFullNames);

                return;
            }

            if (_serviceBusConfiguration.IsWorker || _configuration.Subscribe == SubscribeOption.Ignore)
            {
                return;
            }

            if (!_serviceBusConfiguration.HasInbox
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
                    if (_configuration.Subscribe == SubscribeOption.Normal)
                    {
                        _databaseGateway.ExecuteUsing(
                            RawQuery.Create(
                                    _scriptProvider.Get(Script.SubscriptionManagerSubscribe))
                                .AddParameterValue(SubscriptionManagerColumns.InboxWorkQueueUri,
                                    _serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString())
                                .AddParameterValue(SubscriptionManagerColumns.MessageType, messageType));
                    }
                    else
                    {
                        if (_databaseGateway.GetScalarUsing<int>(
                                RawQuery.Create(
                                        _scriptProvider.Get(Script.SubscriptionManagerContains))
                                    .AddParameterValue(SubscriptionManagerColumns.InboxWorkQueueUri,
                                        _serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString())
                                    .AddParameterValue(SubscriptionManagerColumns.MessageType, messageType)) == 0)
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

            foreach (var messageType in missingMessageTypes)
            {
                _log.Error(string.Format(Resources.MissingSubscription, messageType));
            }

            throw new ApplicationException(string.Format(Resources.MissingSubscriptionException,
                string.Join(",", missingMessageTypes)));
        }

        public IEnumerable<string> GetSubscribedUris(object message)
        {
            Guard.AgainstNull(message, "message");

            var messageType = message.GetType().FullName ?? string.Empty;

            if (!_subscribersCache.Contains(messageType))
            {
                lock (Padlock)
                {
                    if (!_subscribersCache.Contains(messageType))
                    {
                        DataTable table;

                        using (_databaseContextFactory.Create(_subscriptionProviderName, _subscriptionConnectionString))
                        {
                            table = _databaseGateway.GetDataTableFor(
                                RawQuery.Create(
                                        _scriptProvider.Get(
                                            Script.SubscriptionManagerInboxWorkQueueUris))
                                    .AddParameterValue(SubscriptionManagerColumns.MessageType, messageType));
                        }

                        _subscribersCache.Set(messageType, (from DataRow row in table.Rows
                                select SubscriptionManagerColumns.InboxWorkQueueUri.MapFrom(row))
                            .ToList(), DateTimeOffset.Now.Add(_configuration.CacheTimeout));
                    }
                }
            }

            return (IEnumerable<string>)_subscribersCache.Get(messageType);
        }

        private void ServiceBus_Started(object sender, EventArgs e)
        {
            _deferSubscriptions = false;

            if (HasDeferredSubscriptions)
            {
                Subscribe(_deferredSubscriptions);
            }
        }
    }
}