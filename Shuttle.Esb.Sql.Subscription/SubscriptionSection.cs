using System;
using System.Configuration;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Subscription
{
	public class SubscriptionSection : ConfigurationSection
	{
		[ConfigurationProperty("subscriptionManagerConnectionStringName", IsRequired = false,
			DefaultValue = "Subscription")]
		public string SubscriptionManagerConnectionStringName
		{
			get { return (string) this["subscriptionManagerConnectionStringName"]; }
		}

		[ConfigurationProperty("ignoreSubscribe", IsRequired = false, DefaultValue = false)]
		public bool IgnoreSubscribe
		{
			get { return (bool) this["ignoreSubscribe"]; }
		}

		public static SubscriptionConfiguration Configuration()
		{
			var section = ConfigurationSectionProvider.Open<SubscriptionSection>("shuttle", "subscription");
			var configuration = new SubscriptionConfiguration();

			var subscriptionManagerConnectionStringName = "Subscription";

			if (section != null)
			{
				subscriptionManagerConnectionStringName = section.SubscriptionManagerConnectionStringName;
				configuration.IgnoreSubscribe = section.IgnoreSubscribe;
			}

			configuration.SubscriptionManagerProviderName = GetSettings(subscriptionManagerConnectionStringName).ProviderName;
			configuration.SubscriptionManagerConnectionString = GetSettings(subscriptionManagerConnectionStringName).ConnectionString;

			return configuration;
		}

		private static ConnectionStringSettings GetSettings(string connectionStringName)
		{
			var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

			if (settings == null)
			{
				throw new InvalidOperationException(string.Format(SubscriptionResources.ConnectionStringMissing, connectionStringName));
			}

			return settings;
		}
	}
}