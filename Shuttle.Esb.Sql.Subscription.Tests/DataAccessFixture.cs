using System;
using System.Transactions;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Transactions;
#if NETCOREAPP
using System.Data.Common;
using System.Data.SqlClient;
#endif

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [SetUpFixture]
    public class DataAccessFixture
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
#if NETCOREAPP
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
#endif
            
            var connectionConfigurationProvider = new Mock<IConnectionConfigurationProvider>();

            connectionConfigurationProvider.Setup(m => m.Get(It.IsAny<string>())).Returns(
                new ConnectionConfiguration(
                    "Shuttle",
                    "System.Data.SqlClient",
                    "server=.\\sqlexpress;database=shuttle;Integrated Security=sspi;"));


            DatabaseContextFactory = new DatabaseContextFactory(
                connectionConfigurationProvider.Object,
                new DbConnectionFactory(), 
                new DbCommandFactory(), 
                new ThreadStaticDatabaseContextCache());
            
            DatabaseGateway = new DatabaseGateway();

            TransactionScopeFactory =
                new DefaultTransactionScopeFactory(true, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(120));

            DatabaseContextFactory.ConfigureWith("Shuttle");
        }

        public DatabaseGateway DatabaseGateway { get; private set; }
        public DatabaseContextFactory DatabaseContextFactory { get; private set; }
        public static ITransactionScopeFactory TransactionScopeFactory { get; private set; }
    }
}