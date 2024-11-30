using System;

namespace Shuttle.Esb.Sql.Subscription;

public class SqlSubscriptionOptions
{
    public const string SectionName = "Shuttle:SqlSubscription";

    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public string ConnectionStringName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
}