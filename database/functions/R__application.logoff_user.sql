drop function if exists application.logoff_user;

create function application.logoff_user
(
	_email_address varchar(200),
	_user_session_id integer
)
returns void
as $$
declare
	_user_id integer;
begin

	--------------------------------------------
	-- clean parameters
	--
	_email_address = trim(coalesce(_email_address, ''));

	select 
		u.user_id into _user_id
	from application.user u
	inner join application.user_session s on u.user_id = s.user_id
	where lower(u.email_address) = lower(_email_address)
	and s.user_session_id = _user_session_id;

	if (_user_id is null)
	then
		raise exception '_user_session_id and matching _email_address combination not found';
		return;
	end if;

	--------------------------------------------
	-- end the session
	--
	if exists
	(
		select *
		from application.user_session us
		where us.user_session_id = _user_session_id
		and us.end_time is null
	)
	then
		update application.user_session
		set
			end_time = now()
		where user_session_id = _user_session_id;

		-- audit logoff
		perform
		from audit.add_entry
		(
			_user_id := _user_id,
			_user_session_id := _user_session_id,
			_entry_type_id := 3
		);
	end if;

end;
$$ language plpgsql;