/*

	WARNING THIS WILL DELETE EVERYTHING - DATA, TABLES, FUNCTIONS AND USERS

	drop schema application cascade;
	drop schema audit cascade;
	drop schema logging cascade;
	drop schema configuration cascade;
    
	drop table if exists flyway_schema_history;
	
	drop user app_user;
	revoke all privileges on schema public from readonly_user;
	drop user readonly_user;
	drop user dbpatcher_user;
*/
