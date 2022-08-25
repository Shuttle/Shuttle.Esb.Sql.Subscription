using System;

namespace Shuttle.Esb.Sql.Subscription
{
	public interface ISubscriptionConfiguration
	{
		string ProviderName { get; }
		string ConnectionString { get; }
		SubscribeOption Subscribe { get; }
		TimeSpan CacheTimeout { get; }
	}
}