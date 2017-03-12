namespace Shuttle.Esb.Sql.Subscription
{
	public interface ISubscriptionConfiguration
	{
		string SubscriptionManagerProviderName { get; }
		string SubscriptionManagerConnectionString { get; }
		bool IgnoreSubscribe { get; }
	}
}