-- drop all objects
drop table audit.project_logs;
drop table audit.user_logs;
drop table audit.project_activity_logs;
drop table public.documents;
drop table public.note_labels;
drop table public.notes;
drop table public.tasks;
drop table public.tickets;
drop table public.labels;
drop table public.user_firebase_identities;
drop table public.user_projects;
drop table public.projects;
drop table public.search;
drop table public.users;

-- clear all data
delete from audit.project_logs;
delete from user_projects;
delete from projects;
delete from user_firebase_identities;
delete from users;