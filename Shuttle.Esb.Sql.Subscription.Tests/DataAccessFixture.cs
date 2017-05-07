using NUnit.Framework;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
    [SetUpFixture]
    public class DataAccessFixture
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            DatabaseContextFactory = new DatabaseContextFactory(new DbConnectionFactory(), new DbCommandFactory(), new ThreadStaticDatabaseContextCache());
            DatabaseGateway = new DatabaseGateway();

            DatabaseContextFactory.ConfigureWith("Shuttle");
        }

        public DatabaseGateway DatabaseGateway { get; private set; }
        public DatabaseContextFactory DatabaseContextFactory { get; private set; }
    }
}