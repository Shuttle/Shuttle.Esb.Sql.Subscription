using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [TestFixture]
    public class SubscriptionManagerFixture : DataAccessFixture
    {
        private const string WorkQueueUri = "queue://./work";
        private const string ProviderName = "System.Data.SqlClient";
        private const string ConnectionString = "server=.\\sqlexpress;database=shuttle;Integrated Security=sspi;";

        public void ClearSubscriptions()
        {
            using (DatabaseContextFactory.Create(ProviderName, ConnectionString))
            {
                DatabaseGateway.ExecuteUsing(RawQuery.Create("delete from SubscriberMessageType"));
            }
        }

        [Test]
        public void Should_be_able_subscribe_normally()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionManager(SubscribeOption.Normal);

                subscriptionManager.Subscribe<MessageTypeOne>();

                var uris = subscriptionManager.GetSubscribedUris(new MessageTypeOne()).ToList();

                Assert.AreEqual(1, uris.Count);
                Assert.AreEqual(WorkQueueUri, uris.ElementAt(0));
            }
        }

        [Test]
        public void Should_be_able_to_ignore_subscribe()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionManager(SubscribeOption.Ignore);

                subscriptionManager.Subscribe<MessageTypeOne>();

                var uris = subscriptionManager.GetSubscribedUris(new MessageTypeOne()).ToList();

                Assert.AreEqual(0, uris.Count);
            }
        }

        [Test]
        public void Should_be_able_to_ensure_subscribe()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionManager(SubscribeOption.Ensure);

                Assert.Throws<ApplicationException>(() => subscriptionManager.Subscribe<MessageTypeOne>());
            }
        }

        [Test]
        public void Should_be_able_subscribe_normally_using_specified_connection()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionManager(SubscribeOption.Normal);

                subscriptionManager.Subscribe<MessageTypeOne>(ProviderName, ConnectionString);

                var uris = subscriptionManager.GetSubscribedUris(new MessageTypeOne()).ToList();

                Assert.AreEqual(1, uris.Count);
                Assert.AreEqual(WorkQueueUri, uris.ElementAt(0));
            }
        }

        [Test]
        public void Should_be_able_to_ignore_subscribe_using_specified_connection()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionManager(SubscribeOption.Ignore);

                subscriptionManager.Subscribe<MessageTypeOne>(ProviderName, ConnectionString);

                var uris = subscriptionManager.GetSubscribedUris(new MessageTypeOne()).ToList();

                Assert.AreEqual(0, uris.Count);
            }
        }

        [Test]
        public void Should_be_able_to_ensure_subscribe_using_specified_connection()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionManager(SubscribeOption.Ensure);

                Assert.Throws<ApplicationException>(() => subscriptionManager.Subscribe<MessageTypeOne>(ProviderName, ConnectionString));
            }
        }

        private SubscriptionManager GetSubscriptionManager(SubscribeOption subscribe)
        {
            var workQueue = new Mock<IQueue>();

            workQueue.Setup(m => m.Uri).Returns(new Uri(WorkQueueUri));

            var serviceBusConfiguration = new ServiceBusConfiguration
            {
                Inbox = new InboxQueueConfiguration
                {
                    WorkQueue = workQueue.Object
                }
            };

            var subscriptionConfiguration = new SubscriptionConfiguration
            {
                ConnectionString = ConnectionString,
                ProviderName = ProviderName,
                Subscribe = subscribe
            };

            subscriptionConfiguration.Subscribe = subscribe;

            var serviceBusEvents = new ServiceBusEvents();

            var subscriptionManager = new SubscriptionManager(serviceBusEvents,
                serviceBusConfiguration, subscriptionConfiguration,
                new ScriptProvider(new ScriptProviderConfiguration()), DatabaseContextFactory, DatabaseGateway);

            serviceBusEvents.OnStarted(this, new PipelineEventEventArgs(new Mock<IPipelineEvent>().Object));

            return subscriptionManager;
        }
    }
}