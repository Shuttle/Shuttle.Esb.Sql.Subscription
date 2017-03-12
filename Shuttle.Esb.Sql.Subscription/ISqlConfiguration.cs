namespace Shuttle.Esb.Sql.Subscription
{
	public interface ISqlConfiguration
	{
		string SubscriptionManagerProviderName { get; }
		string SubscriptionManagerConnectionString { get; }
		bool IgnoreSubscribe { get; }
	}
}