using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription;

public interface IQueryFactory
{
    IQuery Create();
    IQuery Subscribe(string messageType);
    IQuery Contains(string messageType);
    IQuery GetSubscribedUris(string messageType);
}