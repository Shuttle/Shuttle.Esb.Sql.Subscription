CREATE TABLE public.subscriber_message_type
(
  message_type character varying(250) NOT NULL,
  inbox_work_queue_uri character varying(130) NOT NULL,
  CONSTRAINT pk_subscriber_message_type PRIMARY KEY (message_type, inbox_work_queue_uri)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE public.subscriber_message_type
  OWNER TO postgres;