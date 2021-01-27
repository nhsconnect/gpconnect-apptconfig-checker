drop function if exists caching.update_cache_item;

create function caching.update_cache_item
(
	_dist_cache_id text,
	_utc_now timestamp with time zone
)
returns void
as $$
begin
	update
		caching.dist_cache 
	set 
		ExpiresAtTime = 
		case when 
			(select caching.date_diff('seconds', _utc_now, AbsoluteExpiration) <= SlidingExpirationInSeconds) 
			then AbsoluteExpiration 
			else _utc_now + SlidingExpirationInSeconds * interval '1 second' 
		end
	where
		Id = _dist_cache_id
		and _utc_now <= ExpiresAtTime
		and SlidingExpirationInSeconds is not null
		and (AbsoluteExpiration is null or AbsoluteExpiration <> ExpiresAtTime);
end;
$$ language plpgsql;