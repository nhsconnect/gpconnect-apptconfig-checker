drop function if exists caching.set_cache;

create function caching.set_cache
(
	_dist_cache_id text,
	_dist_cache_value bytea,
	_dist_cache_sliding_expiration_seconds double precision,
	_dist_cache_absolute_expiration timestamp with time zone,
	_utc_now timestamp with time zone
)
returns void
as $$
declare _expires_at_time timestamp(6) with time zone;
declare _row_count integer;
begin
	case
		when (_dist_cache_sliding_expiration_seconds is null)
		then  _expires_at_time := _dist_cache_absolute_expiration; 
	else
		_expires_at_time := _utc_now + _dist_cache_sliding_expiration_seconds * interval '1 second';
	end case;

	update
		caching.dist_cache 
	set 
		Value = _dist_cache_value, 
		ExpiresAtTime = _expires_at_time,
		SlidingExpirationInSeconds = _dist_cache_sliding_expiration_seconds,
		AbsoluteExpiration = _dist_cache_absolute_expiration
	where
		Id = _dist_cache_id;

	get diagnostics _row_count := ROW_COUNT;
	
	if(_row_count = 0) then	
		insert into caching.dist_cache
		(
			Id,
			Value,
			ExpiresAtTime,
			SlidingExpirationInSeconds,
			AbsoluteExpiration
		)
		values
		(
			_dist_cache_id,
			_dist_cache_value,
			_expires_at_time,
			_dist_cache_sliding_expiration_seconds,
			_dist_cache_absolute_expiration
		);
	end if;
end;
$$ language plpgsql;