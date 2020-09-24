create or replace function configuration.get_sds_queries
(
)
returns table
(
	query_name varchar(100),
    search_base varchar(200),
    query_text varchar(1000)
)
as $$
begin

	return query
	select
	    q.query_name,
    	q.search_base,
    	q.query_text
	from configuration.sds_query q;
	
end;
$$ language plpgsql;
