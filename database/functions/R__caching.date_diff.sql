drop function if exists caching.date_diff;

create function caching.date_diff
(
	_units character varying,
	_start_t timestamp with time zone,
	_end_t timestamp with time zone
)
returns integer
as $$
declare _diff_interval interval;
declare _diff integer = 0;
declare _years_diff integer = 0;
begin
	if(_units in ('yy', 'yyyy', 'year', 'mm', 'm', 'month')) then
		_years_diff = date_part('year', _end_t) - datepart('year', _start_t);

		if(_units in ('yy', 'yyyy', 'year')) then
			return _years_diff;
		else
			return _years_diff * 12 + (date_part('month', _end_t) - date_part('month', _start_t));
		end if;
	end if;

	_diff_interval = _end_t - _start_t;
	_diff = _diff + date_part('day', _diff_interval);

	if(_units in ('wk', 'ww', 'week')) then
		_diff = _diff / 7;
		return _diff;
	end if;

	if(_units in ('dd', 'd', 'day')) then
		return _diff;
	end if;

	_diff = _diff * 24 + date_part('hour', _diff_interval);

	if(_units in ('hh', 'hour')) then
		return _diff;
	end if;

	_diff = _diff * 60 + date_part('minute', _diff_interval);

	if(_units in ('mi', 'n', 'minute')) then
		return _diff;
	end if;

	_diff = _diff * 60 + date_part('second', _diff_interval);

	return _diff;
end;
$$ language plpgsql;