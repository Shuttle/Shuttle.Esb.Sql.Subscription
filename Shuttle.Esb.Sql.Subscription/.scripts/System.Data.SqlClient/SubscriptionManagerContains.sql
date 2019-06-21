if exists (select null from [dbo].[SubscriberMessageType] where InboxWorkQueueUri = @InboxWorkQueueUri and MessageType = @MessageType)
	select 1
else
	select 0