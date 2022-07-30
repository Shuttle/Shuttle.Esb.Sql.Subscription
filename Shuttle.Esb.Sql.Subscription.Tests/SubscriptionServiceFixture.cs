using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [TestFixture]
    public class SubscriptionServiceFixture : DataAccessFixture
    {
        private const string WorkQueueUri = "queue://./work";

        public void ClearSubscriptions()
        {
            using (DatabaseContextFactory.Create(ProviderName, ConnectionString))
            {
                DatabaseGateway.Execute(RawQuery.Create("delete from SubscriberMessageType"));
            }
        }

        [Test]
        public void Should_be_able_subscribe_normally()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionService(SubscribeType.Normal);

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

                var subscriptionManager = GetSubscriptionService(SubscribeType.Ignore);

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

                var subscriptionManager = GetSubscriptionService(SubscribeType.Ensure);

                Assert.Throws<ApplicationException>(() => subscriptionManager.Subscribe<MessageTypeOne>());
            }
        }

        [Test]
        public void Should_be_able_subscribe_normally_using_specified_connection()
        {
            using (TransactionScopeFactory.Create())
            {
                ClearSubscriptions();

                var subscriptionManager = GetSubscriptionService(SubscribeType.Normal);

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

                var subscriptionManager = GetSubscriptionService(SubscribeType.Ignore);

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

                var subscriptionManager = GetSubscriptionService(SubscribeType.Ensure);

                Assert.Throws<ApplicationException>(() =>
                    subscriptionManager.Subscribe<MessageTypeOne>(ProviderName, ConnectionString));
            }
        }

        private SubscriptionService GetSubscriptionService(SubscribeType subscribeType)
        {
            var workQueue = new Mock<IQueue>();

            workQueue.Setup(m => m.Uri).Returns(new QueueUri(WorkQueueUri));

            var queueService = new Mock<IQueueService>();

            queueService.Setup(m => m.Get(It.IsAny<string>())).Returns(workQueue.Object);

            var serviceBusConfiguration = new ServiceBusConfiguration(queueService.Object);

            serviceBusConfiguration.Configure(new ServiceBusOptions
            {
                Inbox = new InboxOptions
                {
                    WorkQueueUri = WorkQueueUri
                }
            });

            var connectionStringOptions = new Mock<IOptionsMonitor<ConnectionStringOptions>>();

            connectionStringOptions.Setup(m => m.Get(It.IsAny<string>())).Returns(new ConnectionStringOptions
            {
                Name = "shuttle",
                ProviderName = ProviderName,
                ConnectionString = ConnectionString
            });

            var subscriptionOptions = Options.Create(new SubscriptionOptions
            {
                SubscribeType = subscribeType
            });

            var scriptProviderOptions = Options.Create(new ScriptProviderOptions());

            var pipelineFactory = new Mock<IPipelineFactory>();

            var subscriptionService = new SubscriptionService(connectionStringOptions.Object, subscriptionOptions,
                serviceBusConfiguration, pipelineFactory.Object,
                new ScriptProvider(scriptProviderOptions, DatabaseContextCache), DatabaseContextFactory,
                DatabaseGateway);

            subscriptionService.Execute(new OnStarted());

            return subscriptionService;
        }
    }
}