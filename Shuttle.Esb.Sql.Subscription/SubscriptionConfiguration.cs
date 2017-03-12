namespace Shuttle.Esb.Sql.Subscription
{
	public class SubscriptionConfiguration : ISubscriptionConfiguration
	{
		public SubscriptionConfiguration()
		{
		    IgnoreSubscribe = false;
		}

	    public string ProviderName { get; set; }
	    public string ConnectionString { get; set; }
		public bool IgnoreSubscribe { get; set; }
	}
}