using System;

namespace Shuttle.Esb.Sql.Subscription
{
    public enum SubscribeOption
    {
        Normal = 0,
        Ensure = 1,
        Ignore = 2
    }

    public class SubscriptionConfiguration : ISubscriptionConfiguration
	{
		public SubscriptionConfiguration()
		{
		    Subscribe = SubscribeOption.Normal;
		    CacheTimeout = TimeSpan.FromMinutes(5);
		}

	    public string ProviderName { get; set; }
	    public string ConnectionString { get; set; }
		public SubscribeOption Subscribe { get; set; }
		public TimeSpan CacheTimeout { get; set; }
	}
}