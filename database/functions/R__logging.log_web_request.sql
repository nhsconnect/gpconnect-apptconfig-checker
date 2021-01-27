drop function if exists logging.log_web_request;

create function logging.log_web_request
(
	_user_id integer,
	_user_session_id integer,
	_url varchar(1000),
	_referrer_url varchar(1000),
	_description varchar(1000),
	_ip varchar(255),
	_created_date timestamp,
	_created_by varchar(255),
	_server varchar(255),
	_response_code integer,
	_session_id varchar(1000),
	_user_agent varchar(1000)
)
returns void
as $$
begin

	insert into logging.web_request
	(
		user_id,
		user_session_id,
		url,
		referrer_url,
		description,
		ip,
		created_date,
		created_by,
		server,
		response_code,
		session_id,
		user_agent
	)
	values
	(
		_user_id,
		_user_session_id,
		_url,
		_referrer_url,
		_description,
		_ip,
		_created_date,
		_created_by,
		_server,
		_response_code,
		_session_id,
		_user_agent
	);

end;
$$ language plpgsql;