using System;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription
{
    public class SubscriptionBuilder
    {
        private SubscriptionOptions _subscriptionOptions = new SubscriptionOptions();
        public IServiceCollection Services { get; }

        public SubscriptionOptions Options
        {
            get => _subscriptionOptions;
            set => _subscriptionOptions = value ?? throw new ArgumentNullException(nameof(value));
        }

        public SubscriptionBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));

            Services = services;
        }
    }
}