using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription;

public class SqlSubscriptionBuilder
{
    private SqlSubscriptionOptions _sqlSubscriptionOptions = new();

    public SqlSubscriptionBuilder(IServiceCollection services)
    {
        Services = Guard.AgainstNull(services);
    }

    public SqlSubscriptionOptions Options
    {
        get => _sqlSubscriptionOptions;
        set => _sqlSubscriptionOptions = Guard.AgainstNull(value);
    }

    public IServiceCollection Services { get; }

    public SqlSubscriptionBuilder UseSqlServer()
    {
        Services.AddSingleton<IQueryFactory, SqlServer.QueryFactory>();

        return this;
    }

    public SqlSubscriptionBuilder UseNpgsql()
    {
        Services.AddSingleton<IQueryFactory, Npgsql.QueryFactory>();

        return this;
    }
}