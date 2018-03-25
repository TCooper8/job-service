create table jobs (
  id uuid primary key default gen_random_uuid(),
  created_on timestamp without time zone not null default (now() at time zone 'utc'),
  due_by timestamp without time zone not null,
  priority int not null,
  accepted_on timestamp without time zone,
  updated_on timestamp without time zone,
  completed_on timestamp without time zone,
  exchange_id uuid not null references exchanges(id) on delete cascade,
  subject varchar(128) not null,
  content_type varchar(128) not null,
  body bytea not null
);