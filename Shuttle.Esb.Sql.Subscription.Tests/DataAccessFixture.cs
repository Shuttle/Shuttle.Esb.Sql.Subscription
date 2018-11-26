using System;
using System.Transactions;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Transactions;
#if (NETCOREAPP2_0 || NETCOREAPP2_1 || NETSTANDARD2_0)
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
            var connectionConfigurationProvider = new Mock<IConnectionConfigurationProvider>();

            connectionConfigurationProvider.Setup(m => m.Get(It.IsAny<string>())).Returns(
                new ConnectionConfiguration(
                    "Shuttle",
                    "System.Data.SqlClient",
                    "server=.\\sqlexpress;database=shuttle;Integrated Security=sspi;"));


#if (!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETSTANDARD2_0)
            DatabaseContextFactory = new DatabaseContextFactory(
                connectionConfigurationProvider.Object,
                new DbConnectionFactory(), 
                new DbCommandFactory(), 
                new ThreadStaticDatabaseContextCache());
#else
            DatabaseContextFactory = new DatabaseContextFactory(
                connectionConfigurationProvider.Object,
                new DbConnectionFactory(new DbProviderFactories()),
                new DbCommandFactory(),
                new ThreadStaticDatabaseContextCache());
#endif
            
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