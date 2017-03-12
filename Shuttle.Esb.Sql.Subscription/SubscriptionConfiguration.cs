namespace Shuttle.Esb.Sql.Subscription
{
	public class SubscriptionConfiguration : ISubscriptionConfiguration
	{
		public SubscriptionConfiguration()
		{
		    IgnoreSubscribe = false;
		}

	    public string SubscriptionManagerProviderName { get; set; }
	    public string SubscriptionManagerConnectionString { get; set; }
		public bool IgnoreSubscribe { get; set; }
	}
}