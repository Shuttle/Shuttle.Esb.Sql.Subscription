using System;
using System.Collections.Generic;
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

                var subscriptionManager = ExerciseSubscriptionService(SubscribeType.Normal, new List<string>
                {
                    typeof(MessageTypeOne).FullName
                });

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

                var subscriptionManager = ExerciseSubscriptionService(SubscribeType.Ignore, new List<string>
                {
                    typeof(MessageTypeOne).FullName
                });

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

                Assert.Throws<ApplicationException>(() => ExerciseSubscriptionService(SubscribeType.Ensure, new List<string>
                {
                    typeof(MessageTypeOne).FullName
                }));
            }
        }

        private ISubscriptionService ExerciseSubscriptionService(SubscribeType subscribeType, List<string> messageTypes)
        {
            var workQueue = new Mock<IQueue>();

            workQueue.Setup(m => m.Uri).Returns(new QueueUri(WorkQueueUri));

            var queueService = new Mock<IQueueService>();

            queueService.Setup(m => m.Get(It.IsAny<string>())).Returns(workQueue.Object);

            var connectionStringOptions = new Mock<IOptionsMonitor<ConnectionStringOptions>>();

            connectionStringOptions.Setup(m => m.Get(It.IsAny<string>())).Returns(new ConnectionStringOptions
            {
                Name = "shuttle",
                ProviderName = ProviderName,
                ConnectionString = ConnectionString
            });

            var serviceBusOptions = new ServiceBusOptions
            {
                Inbox = new InboxOptions
                {
                    WorkQueueUri = WorkQueueUri
                },
                SubscriptionOptions = new SubscriptionOptions
                {
                    SubscribeType = subscribeType,
                    ConnectionStringName = "shuttle",
                    MessageTypes = messageTypes
                }
            };

            var scriptProviderOptions = Options.Create(new ScriptProviderOptions());

            var pipelineFactory = new Mock<IPipelineFactory>();

            var subscriptionService = new SubscriptionService(connectionStringOptions.Object, Options.Create(serviceBusOptions), pipelineFactory.Object,
                new ScriptProvider(scriptProviderOptions, DatabaseContextCache), DatabaseContextFactory,
                DatabaseGateway);

            subscriptionService.Execute(new OnStarted());

            return subscriptionService;
        }
    }
}