using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlSubscription(this IServiceCollection services)
    {
        Guard.AgainstNull(services);

        services.TryAddSingleton<IScriptProvider, ScriptProvider>();
        services.AddSingleton<ISubscriptionService, SubscriptionService>();

        return services;
    }
}