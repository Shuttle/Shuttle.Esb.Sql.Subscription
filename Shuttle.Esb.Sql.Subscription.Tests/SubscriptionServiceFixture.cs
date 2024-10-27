using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription.Tests;

[TestFixture]
public class SubscriptionServiceFixture : DataAccessFixture
{
    private const string WorkQueueUri = "queue://./work";

    public async Task ClearSubscriptionsAsync()
    {
        await using (var databaseContext = DatabaseContextFactory.Create("shuttle"))
        {
            await databaseContext.ExecuteAsync(new Query("delete from SubscriberMessageType"));
        }
    }

    [Test]
    public async Task Should_be_able_subscribe_normally_async()
    {
        using (TransactionScopeFactory.Create())
        {
            await ClearSubscriptionsAsync();

            var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscribeType.Normal, new()
            {
                typeof(MessageTypeOne).FullName!
            });

            var uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();

            Assert.That(uris.Count, Is.EqualTo(1));
            Assert.That(uris.ElementAt(0), Is.EqualTo(WorkQueueUri));
        }
    }

    [Test]
    public async Task Should_be_able_to_ignore_subscribe_async()
    {
        using (TransactionScopeFactory.Create())
        {
            await ClearSubscriptionsAsync();

            var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscribeType.Ignore, new()
            {
                typeof(MessageTypeOne).FullName!
            });

            var uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();

            Assert.That(uris.Count, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Should_be_able_to_ensure_subscribe_async()
    {
        using (TransactionScopeFactory.Create())
        {
            await ClearSubscriptionsAsync();

            Assert.ThrowsAsync<ApplicationException>(async () => await ExerciseSubscriptionServiceAsync(SubscribeType.Ensure, new()
            {
                typeof(MessageTypeOne).FullName!
            }));
        }
    }

    private async Task<ISubscriptionService> ExerciseSubscriptionServiceAsync(SubscribeType subscribeType, List<string> messageTypes)
    {
        var workQueue = new Mock<IQueue>();

        workQueue.Setup(m => m.Uri).Returns(new QueueUri(WorkQueueUri));

        var queueService = new Mock<IQueueService>();

        queueService.Setup(m => m.Get(It.IsAny<Uri>())).Returns(workQueue.Object);

        var connectionStringOptions = new Mock<IOptionsMonitor<ConnectionStringOptions>>();

        connectionStringOptions.Setup(m => m.Get(It.IsAny<string>())).Returns(new ConnectionStringOptions
        {
            Name = "shuttle",
            ProviderName = ProviderName,
            ConnectionString = ConnectionString
        });

        var serviceBusOptions = new ServiceBusOptions
        {
            Inbox = new()
            {
                WorkQueueUri = WorkQueueUri
            },
            Subscription = new()
            {
                SubscribeType = subscribeType,
                ConnectionStringName = "shuttle",
                MessageTypes = messageTypes
            }
        };

        var scriptProviderOptions = Options.Create(new ScriptProviderOptions());

        var pipelineFactory = new Mock<IPipelineFactory>();

        var subscriptionService = new SubscriptionService(connectionStringOptions.Object, Options.Create(serviceBusOptions), pipelineFactory.Object, new ScriptProvider(connectionStringOptions.Object, scriptProviderOptions), DatabaseContextFactory);

        await subscriptionService.ExecuteAsync(new PipelineContext<OnStarted>(new Pipeline()));

        return subscriptionService;
    }
}