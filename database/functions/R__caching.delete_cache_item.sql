drop function if exists caching.delete_cache_item;

create function caching.delete_cache_item
(
	_dist_cache_id text
)
returns void
as $$
begin
	delete from 
		caching.dist_cache 
	where
		Id = _dist_cache_id;
end;
$$ language plpgsql;