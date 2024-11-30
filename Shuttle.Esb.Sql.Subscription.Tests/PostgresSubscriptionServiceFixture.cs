using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Subscription.Tests;

[TestFixture]
public class PostgresSubscriptionServiceFixture
{
    private const string WorkQueueUri = "queue://./work";

    [Test]
    public async Task Should_be_able_subscribe_normally_async()
    {
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            var subscriptionService = await ExerciseSubscriptionServiceAsync(SubscribeType.Normal, [typeof(MessageTypeOne).FullName!]);

            var uris = (await subscriptionService.GetSubscribedUrisAsync(new MessageTypeOne())).ToList();

            Assert.That(uris.Count, Is.EqualTo(1));
            Assert.That(uris.ElementAt(0), Is.EqualTo(WorkQueueUri));
        }
    }

    [Test]
    public async Task Should_be_able_to_ignore_subscribe_async()
    {
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
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
    }

    [Test]
    public void Should_be_able_to_ensure_subscribe()
    {
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            Assert.ThrowsAsync<ApplicationException>(async () => await ExerciseSubscriptionServiceAsync(SubscribeType.Ensure, [typeof(MessageTypeOne).FullName!]));
        }
    }

    private async Task<ISubscriptionService> ExerciseSubscriptionServiceAsync(SubscribeType subscribeType, List<string> messageTypes)
    {
        DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

        var services = new ServiceCollection()
                .AddDataAccess(builder =>
                {
                    builder.AddConnectionString("Shuttle", "Npgsql", "Host=localhost;Port=5432;Username=postgres;Password=Pass!000;Database=Shuttle");
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
                    builder.Options.Schema = "subscription_fixture";

                    builder.UseNpgsql();
                })
            ;

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            await using (var databaseContext = serviceProvider.GetRequiredService<IDatabaseContextFactory>().Create("Shuttle"))
            {
                await databaseContext.ExecuteAsync(new Query(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'subscription_fixture' AND table_name = 'subscriber_message_type') THEN
        DELETE FROM subscription_fixture.subscriber_message_type;
    END IF;
END
$$;
"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        var subscriptionService = serviceProvider.GetRequiredService<ISubscriptionService>();

        await ((SubscriptionService)subscriptionService).ExecuteAsync(new PipelineContext<OnStarted>(new Pipeline(serviceProvider)));

        return subscriptionService;
    }
}