drop function if exists logging.purge_logs;

create function logging.purge_logs
(
)
returns table
(
	error_log_deleted_count integer,
	spine_message_deleted_count integer,
	web_request_deleted_count integer
)
as $$
declare
	_log_retention_days integer;
	_error_log_deleted_count integer;
	_spine_message_deleted_count integer;
	_web_request_deleted_count integer;
begin

	select 
		log_retention_days into _log_retention_days 
	from configuration.general;

	if (_log_retention_days is null)
	then
		raise exception 'configuration.general.log_retention_days is not set';
		return;
	end if;

	with deleted as
	(
		delete
		from logging.error_log
		where logged < (now() - (interval '1' day * _log_retention_days))
		returning id
	)
	select
		count(*) into _error_log_deleted_count
	from deleted;

	with deleted as
	( 
		delete
		from logging.spine_message 
		where logged_date < (now() - (interval '1' day * _log_retention_days))
		returning spine_message_id
	)
	select
		count(*) into _spine_message_deleted_count
	from deleted;

	with deleted as
	(
		delete
		from logging.web_request
		where created_date < (now() - (interval '1' day * _log_retention_days))
		returning web_request_id
	)
	select
		count(*) into _web_request_deleted_count
	from deleted;

	return query
	select
		_error_log_deleted_count as error_log_deleted_count, 
		_spine_message_deleted_count as spine_message_deleted_count,
		_web_request_deleted_count as web_request_deleted_count;
	
end;
$$ language plpgsql;