using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using System;

namespace Shuttle.Esb.Sql.Subscription;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlSubscription(this IServiceCollection services, Action<SqlSubscriptionBuilder>? builder = null)
    {
        var sqlSubscriptionBuilder = new SqlSubscriptionBuilder(Guard.AgainstNull(services));

        builder?.Invoke(sqlSubscriptionBuilder);

        services.AddSingleton<IValidateOptions<SqlSubscriptionOptions>, SqlSubscriptionOptionsValidator>();

        services.AddOptions<SqlSubscriptionOptions>().Configure(options =>
        {
            options.ConnectionStringName = sqlSubscriptionBuilder.Options.ConnectionStringName;
            options.Schema = sqlSubscriptionBuilder.Options.Schema;
            options.CacheTimeout = sqlSubscriptionBuilder.Options.CacheTimeout;
        });
        
        services.AddSingleton<ISubscriptionService, SubscriptionService>();

        return services;
    }
}