create table reports (
  job_id uuid not null references jobs(id) on delete cascade,
  page serial not null,
  created_on timestamp without time zone not null,
  title varchar(128) not null,
  body bytea not null,
  unique(job_id, page)
);