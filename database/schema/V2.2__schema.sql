-- create user
create user dbpatcher_user;
alter user dbpatcher_user valid until 'infinity';

-- grant all permissions
do
$$
declare 
  dbname text := current_database();
begin
  execute format('grant all privileges on database "%s" to dbpatcher_user', dbname);
end
$$;
