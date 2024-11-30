using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription.Npgsql;

public class QueryFactory : IQueryFactory
{
    private readonly string _schema;
    private readonly string _inboxWorkQueueUri;

    public QueryFactory(IOptions<ServiceBusOptions> serviceBusOptions, IOptions<SqlSubscriptionOptions> sqlSubscriptionOptions)
    {
        _inboxWorkQueueUri = Guard.AgainstNullOrEmptyString(Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value).Inbox.WorkQueueUri);
        _schema = Guard.AgainstNull(Guard.AgainstNull(sqlSubscriptionOptions).Value).Schema;
    }

    public IQuery Create()
    {
        return new Query($@"
CREATE SCHEMA IF NOT EXISTS {_schema};

CREATE TABLE IF NOT EXISTS {_schema}.subscriber_message_type (
    message_type VARCHAR(250) NOT NULL,
    inbox_work_queue_uri VARCHAR(250) NOT NULL,
    CONSTRAINT pk_subscriber_message_type PRIMARY KEY (message_type, inbox_work_queue_uri)
);
");
    }

    public IQuery Subscribe(string messageType)
    {
        return new Query($@"
INSERT INTO {_schema}.subscriber_message_type 
(
    message_type,
    inbox_work_queue_uri
)
VALUES 
(
    @MessageType,
    @InboxWorkQueueUri
)
ON CONFLICT (message_type, inbox_work_queue_uri) DO NOTHING;
")
            .AddParameter(Columns.InboxWorkQueueUri, _inboxWorkQueueUri)
            .AddParameter(Columns.MessageType, messageType);
    }

    public IQuery Contains(string messageType)
    {
        return new Query($@"
SELECT (EXISTS (SELECT 1 FROM {_schema}.subscriber_message_type where inbox_work_queue_uri = @InboxWorkQueueUri and message_type = @MessageType))::int
")
            .AddParameter(Columns.InboxWorkQueueUri, _inboxWorkQueueUri)
            .AddParameter(Columns.MessageType, messageType);
    }

    public IQuery GetSubscribedUris(string messageType)
    {
        return new Query($@"
SELECT 
    inbox_work_queue_uri as InboxWorkQueueUri
FROM 
    {_schema}.subscriber_message_type
WHERE 
    message_type = @MessageType
")
            .AddParameter(Columns.MessageType, messageType);
    }
}