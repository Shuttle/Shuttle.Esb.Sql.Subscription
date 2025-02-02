using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription.SqlServer;

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
EXEC sp_getapplock @Resource = '{typeof(QueryFactory).FullName}', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 15000;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_schema}')
BEGIN
    EXEC('CREATE SCHEMA {_schema}');
END

IF OBJECT_ID ('{_schema}.SubscriberMessageType', 'U') IS NULL 
BEGIN
    CREATE TABLE [{_schema}].[SubscriberMessageType]
    (
	    [MessageType] [varchar](250) NOT NULL,
	    [InboxWorkQueueUri] [varchar](250) NOT NULL,
        CONSTRAINT 
            [PK_SubscriberMessageType] 
        PRIMARY KEY CLUSTERED 
        (
	        [MessageType] ASC,
	        [InboxWorkQueueUri] ASC
        )
        WITH 
        (
            PAD_INDEX = OFF, 
            STATISTICS_NORECOMPUTE = OFF, 
            IGNORE_DUP_KEY = OFF, 
            ALLOW_ROW_LOCKS = ON, 
            ALLOW_PAGE_LOCKS = ON
        ) 
        ON [PRIMARY]
    ) 
    ON [PRIMARY]
END

EXEC sp_releaseapplock @Resource = '{typeof(QueryFactory).FullName}', @LockOwner = 'Session';
");
    }

    public IQuery Subscribe(string messageType)
    {
        return new Query($@"
IF NOT EXISTS 
(
    SELECT 
        NULL 
    FROM 
        [{_schema}].[SubscriberMessageType] 
    WHERE 
        InboxWorkQueueUri = @InboxWorkQueueUri 
    AND 
        MessageType = @MessageType
)
	INSERT INTO [{_schema}].[SubscriberMessageType]
	(
        InboxWorkQueueUri, 
        MessageType
    )
	VALUES 
	(
        @InboxWorkQueueUri, 
        @MessageType
    )
")
            .AddParameter(Columns.InboxWorkQueueUri, _inboxWorkQueueUri)
            .AddParameter(Columns.MessageType, messageType);
    }

    public IQuery Contains(string messageType)
    {
        return new Query($@"
IF EXISTS 
(
    SELECT 
        NULL 
    FROM 
        [{_schema}].[SubscriberMessageType] 
    WHERE 
        InboxWorkQueueUri = @InboxWorkQueueUri 
    AND 
        MessageType = @MessageType
)
	SELECT 1
ELSE
	SELECT 0
")
            .AddParameter(Columns.InboxWorkQueueUri, _inboxWorkQueueUri)
            .AddParameter(Columns.MessageType, messageType);
    }

    public IQuery GetSubscribedUris(string messageType)
    {
        return new Query($@"
SELECT 
    InboxWorkQueueUri 
FROM 
    [{_schema}].[SubscriberMessageType] 
WHERE 
    MessageType = @MessageType
")
            .AddParameter(Columns.MessageType, messageType);
    }
}