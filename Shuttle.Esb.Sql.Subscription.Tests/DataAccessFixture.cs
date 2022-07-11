using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Transactions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Transactions;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [SetUpFixture]
    public class DataAccessFixture
    {
        protected const string ProviderName = "System.Data.SqlClient";
        protected const string ConnectionString = "server=.;database=shuttle;user id=sa;password=Pass!000";

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

            var connectionStringOptions = new Mock<IOptionsMonitor<ConnectionStringOptions>>();

            connectionStringOptions.Setup(m => m.Get(It.IsAny<string>())).Returns(new ConnectionStringOptions
            {
                Name = "shuttle",
                ProviderName = ProviderName,
                ConnectionString = ConnectionString
            });

            ConnectionStringOptions = connectionStringOptions.Object;

            DatabaseContextFactory = new DatabaseContextFactory(
                ConnectionStringOptions,
                new DbConnectionFactory(), 
                new DbCommandFactory(Options.Create(new CommandOptions())), 
                new ThreadStaticDatabaseContextCache());

            DatabaseContextCache = new ThreadStaticDatabaseContextCache();

            DatabaseGateway = new DatabaseGateway(DatabaseContextCache);

            TransactionScopeFactory =
                new TransactionScopeFactory(Options.Create(new TransactionScopeOptions
                {
                    Enabled = true,
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(120)
                }));

            DatabaseContextFactory.ConfigureWith("Shuttle");
        }

        public IDatabaseGateway DatabaseGateway { get; private set; }
        public IDatabaseContextCache DatabaseContextCache { get; private set; }
        public IDatabaseContextFactory DatabaseContextFactory { get; private set; }
        public static ITransactionScopeFactory TransactionScopeFactory { get; private set; }
        public IOptionsMonitor<ConnectionStringOptions> ConnectionStringOptions { get; private set; }
    }
}