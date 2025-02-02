using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription.Tests;

[TestFixture]
public class SqlServerSubscriptionServiceFixture
{
    private const string WorkQueueUri = "queue://./work";

    [Test]
    public async Task Should_be_able_subscribe_normally_async()
    {
        var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscribeType.Normal, [typeof(MessageTypeOne).FullName!]);

        var uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();

        Assert.That(uris.Count, Is.EqualTo(1));
        Assert.That(uris.ElementAt(0), Is.EqualTo(WorkQueueUri));
    }

    [Test]
    public async Task Should_be_able_to_ignore_subscribe_async()
    {
        var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscribeType.Ignore, [typeof(MessageTypeOne).FullName!]);

        List<string> uris = [];

        try
        {
            uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();
        }
        catch
        {
            // ignore
        }

        Assert.That(uris.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_be_able_to_ensure_subscribe()
    {
        Assert.ThrowsAsync<ApplicationException>(async () => await ExerciseSubscriptionServiceAsync(SubscribeType.Ensure, [typeof(MessageTypeOne).FullName!]));
    }

    private async Task<ISubscriptionService> ExerciseSubscriptionServiceAsync(SubscribeType subscribeType, List<string> messageTypes)
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

        var services = new ServiceCollection()
            .AddDataAccess(builder =>
            {
                builder.AddConnectionString("Shuttle", "Microsoft.Data.SqlClient", "server=.;database=shuttle;user id=sa;password=Pass!000;TrustServerCertificate=true");
            })
            .AddServiceBus(builder =>
            {
                builder.Options.Inbox.WorkQueueUri = WorkQueueUri;
                builder.Options.Subscription.SubscribeType = subscribeType;
                builder.Options.Subscription.MessageTypes = messageTypes;
            })
            .AddSqlSubscription(builder =>
            {
                builder.Options.ConnectionStringName = "Shuttle";
                builder.Options.Schema = "SubscriptionFixture";

                builder.UseSqlServer();
            });

        var serviceProvider = services.BuildServiceProvider();

        _ = serviceProvider.GetServices<IHostedService>().OfType<SubscriptionHostedService>().First();

        try
        {
            await using (var databaseContext = serviceProvider.GetRequiredService<IDatabaseContextFactory>().Create("Shuttle"))
            {
                await databaseContext.ExecuteAsync(new Query("DELETE FROM SubscriptionFixture.SubscriberMessageType"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        var subscriptionService = serviceProvider.GetRequiredService<ISubscriptionService>();

        await serviceProvider.GetRequiredService<SubscriptionObserver>().ExecuteAsync(new PipelineContext<OnStarted>(new Pipeline(serviceProvider)));

        await serviceProvider.GetServices<IHostedService>().OfType<SubscriptionHostedService>().First().StopAsync(default);

        return subscriptionService;
    }
}