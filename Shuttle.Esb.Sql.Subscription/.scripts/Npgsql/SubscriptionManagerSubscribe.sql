insert into subscriber_message_type (inbox_work_queue_uri, message_type) 
select :InboxWorkQueueUri, :MessageType
where not exists (select 1 
                  from subscriber_message_type 
				  where 
				    inbox_work_queue_uri = :InboxWorkQueueUri 
				    and message_type = :MessageType)

