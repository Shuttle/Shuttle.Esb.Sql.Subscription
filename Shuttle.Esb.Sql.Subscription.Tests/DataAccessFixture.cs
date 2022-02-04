using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Transactions;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Transactions;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [SetUpFixture]
    public class DataAccessFixture
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

            var connectionConfigurationProvider = new Mock<IConnectionConfigurationProvider>();

            connectionConfigurationProvider.Setup(m => m.Get(It.IsAny<string>())).Returns(
                new ConnectionConfiguration(
                    "Shuttle",
                    "System.Data.SqlClient",
                    "server=.;database=shuttle;user id=sa;password=Pass!000"));

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