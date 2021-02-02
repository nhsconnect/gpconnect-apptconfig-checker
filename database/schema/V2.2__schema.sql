create user dbpatcher_user;
alter user dbpatcher_user valid until 'infinity';
grant postgres to dbpatcher_user;