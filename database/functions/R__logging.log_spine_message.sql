drop function if exists logging.log_spine_message;

create function logging.log_spine_message
(
    _user_session_id integer,
    _spine_message_type_id integer,
    _command varchar(8000),
    _request_headers text,
    _request_payload text,
    _response_status varchar(100),
    _response_headers text,
    _response_payload text,
    _roundtriptime_ms integer
)
returns void
as $$
begin

	insert into logging.spine_message
	(
		user_session_id,
		spine_message_type_id,
		command,
		request_headers,
		request_payload,
		response_status,
		response_headers,
		response_payload,
		logged_date,
		roundtriptime_ms
	)
	values
	(
		_user_session_id,
		_spine_message_type_id,
		_command,
		_request_headers,
		_request_payload,
		_response_status,
		_response_headers,
		_response_payload,
		now(),
		_roundtriptime_ms
	);

end;
$$ language plpgsql;