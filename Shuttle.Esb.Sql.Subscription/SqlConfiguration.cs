namespace Shuttle.Esb.Sql.Subscription
{
	public class SqlConfiguration : ISqlConfiguration
	{
		public SqlConfiguration()
		{
		    IgnoreSubscribe = false;
		}

	    public string SubscriptionManagerProviderName { get; set; }
	    public string SubscriptionManagerConnectionString { get; set; }
		public bool IgnoreSubscribe { get; set; }
	}
}