using System;
using System.Configuration;
using Shuttle.Core.Configuration;

namespace Shuttle.Esb.Sql.Subscription
{
    public class SubscriptionSection : ConfigurationSection
    {
        [ConfigurationProperty("connectionStringName", IsRequired = false, DefaultValue = "Subscription")]
        public string ConnectionStringName => (string) this["connectionStringName"];

        [ConfigurationProperty("subscribe", IsRequired = false, DefaultValue = SubscribeOption.Normal)]
        public SubscribeOption Subscribe => (SubscribeOption) this["subscribe"];

        public static SubscriptionConfiguration Configuration()
        {
            var section = ConfigurationSectionProvider.Open<SubscriptionSection>("shuttle", "subscription");
            var configuration = new SubscriptionConfiguration();

            var connectionStringName = "Subscription";

            if (section != null)
            {
                connectionStringName = section.ConnectionStringName;
                configuration.Subscribe = section.Subscribe;
            }

            configuration.ProviderName = GetSettings(connectionStringName).ProviderName;
            configuration.ConnectionString = GetSettings(connectionStringName).ConnectionString;

            return configuration;
        }

        private static ConnectionStringSettings GetSettings(string connectionStringName)
        {
            var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (settings == null)
            {
                throw new InvalidOperationException(string.Format(Resources.ConnectionStringMissing, connectionStringName));
            }

            return settings;
        }
    }
}