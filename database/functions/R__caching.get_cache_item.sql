drop function if exists caching.get_cache_item;

create function caching.get_cache_item
(
	_dist_cache_id text,
	_utc_now timestamp with time zone
)
returns table(Id text, Value bytea, ExpiresAtTime timestamp with time zone, SlidingExpirationInSeconds double precision, AbsoluteExpiration timestamp with time zone)    
as $$
begin
	return query
		select
			dc.Id,
			dc.Value,
			dc.ExpiresAtTime,
			dc.SlidingExpirationInSeconds,
			dc.AbsoluteExpiration
		from
			caching.dist_cache dc
		where
			dc.Id = _dist_cache_id
			and _utc_now <= dc.ExpiresAtTime;
end;
$$ language plpgsql;