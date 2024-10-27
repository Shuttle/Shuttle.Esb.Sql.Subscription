using System;
using System.Data.Common;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Core.Transactions;

namespace Shuttle.Esb.Sql.Subscription.Tests;

[SetUpFixture]
public class DataAccessFixture
{
    protected const string ProviderName = "Microsoft.Data.SqlClient";
    protected const string ConnectionString = "server=.;database=shuttle;user id=sa;password=Pass!000;TrustServerCertificate=true";
    public IOptionsMonitor<ConnectionStringOptions> ConnectionStringOptions { get; private set; }
    public IDatabaseContextFactory DatabaseContextFactory { get; private set; }
    public ITransactionScopeFactory TransactionScopeFactory { get; private set; }

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

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
            Options.Create(new DataAccessOptions
            {
                DatabaseContextFactory = new()
                {
                    DefaultConnectionStringName = "Shuttle"
                }
            }),
            new DbConnectionFactory(),
            new DbCommandFactory(Options.Create(new DataAccessOptions())));

        TransactionScopeFactory =
            new TransactionScopeFactory(Options.Create(new TransactionScopeOptions
            {
                Enabled = true,
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(120)
            }));
    }
}