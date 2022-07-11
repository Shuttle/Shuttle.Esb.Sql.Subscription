using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSubscription(this IServiceCollection services,
            Action<SubscriptionBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var subscriptionBuilder = new SubscriptionBuilder(services);

            builder?.Invoke(subscriptionBuilder);

            services.TryAddSingleton<IScriptProvider, ScriptProvider>();
            services.TryAddSingleton<ISubscriptionService, SubscriptionService>();

            services.AddOptions<SubscriptionOptions>().Configure(options =>
            {
            });

            return services;
        }
    }
}