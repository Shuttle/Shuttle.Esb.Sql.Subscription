using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
#if (NETCOREAPP2_0 || NETSTANDARD2_0)
using Shuttle.Core.Data.SqlClient;
#endif

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [SetUpFixture]
    public class DataAccessFixture
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
#if (!NETCOREAPP2_0 && !NETSTANDARD2_0)
            DatabaseContextFactory = new DatabaseContextFactory(
                new ConnectionConfigurationProvider(),
                new DbConnectionFactory(), 
                new DbCommandFactory(), 
                new ThreadStaticDatabaseContextCache());
#else
            var connectionConfigurationProvider = new Mock<IConnectionConfigurationProvider>();

            connectionConfigurationProvider.Setup(m => m.Get(It.IsAny<string>())).Returns(
                new ConnectionConfiguration(
                    "Shuttle",
                    "System.Data.SqlClient",
                    "Data Source=.\\sqlexpress;Initial Catalog=shuttle;Integrated Security=SSPI;"));

            DatabaseContextFactory = new DatabaseContextFactory(
                connectionConfigurationProvider.Object,
                new DbConnectionFactory(new DbProviderFactories()),
                new DbCommandFactory(),
                new ThreadStaticDatabaseContextCache());
#endif
            
            DatabaseGateway = new DatabaseGateway();

            DatabaseContextFactory.ConfigureWith("Shuttle");
        }

        public DatabaseGateway DatabaseGateway { get; private set; }
        public DatabaseContextFactory DatabaseContextFactory { get; private set; }
    }
}