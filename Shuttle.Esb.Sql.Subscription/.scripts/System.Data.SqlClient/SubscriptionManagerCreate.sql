SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SubscriberMessageType]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SubscriberMessageType](
	[MessageType] [varchar](250) NOT NULL,
	[InboxWorkQueueUri] [varchar](130) NOT NULL,
 CONSTRAINT [PK_SubscriberMessageType] PRIMARY KEY CLUSTERED 
(
	[MessageType] ASC,
	[InboxWorkQueueUri] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
