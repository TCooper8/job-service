create extension if not exists pgcrypto;

create table if not exists exchanges (
  id uuid primary key default gen_random_uuid(),
  name text not null unique
);