drop function if exists configuration.get_sds_queries;

create function configuration.get_sds_queries
(
)
returns table
(
	query_name varchar(100),
	search_base varchar(200),
	query_text varchar(1000),
	query_attributes varchar(500)
)
as $$
begin

	return query
	select
		q.query_name,
		q.search_base,
		q.query_text,
		q.query_attributes
	from configuration.sds_query q;
	
end;
$$ language plpgsql;

