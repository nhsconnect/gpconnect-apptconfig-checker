drop function if exists application.set_user_isauthorised;

create function application.set_user_isauthorised
(
	_email_address varchar(200),
	_is_authorised boolean
)
returns void
as $$
declare
	_user_id integer;
	_is_authorised_existing boolean;
begin

	--------------------------------------------
	-- clean parameters
	--
	_email_address = trim(coalesce(_email_address, ''));

	if (_is_authorised is null)
	then
		raise exception '_is_authorised is null';
		return;
	end if;

	--------------------------------------------
	-- find / create user
	--
	select
		u.user_id, 
		u.is_authorised 
	into
		_user_id,
		_is_authorised_existing
	from application.user u
	where lower(u.email_address) = lower(_email_address);

	--------------------------------------------
	-- run some checks
	--
	if (_user_id is null)
	then
		raise exception 'could not find that user';
		return;
	end if;

	if (_is_authorised and _is_authorised_existing)
	then
		raise exception 'user is already authorised';
		return;
	end if;

	if ((not _is_authorised) and (not _is_authorised_existing))
	then
		raise exception 'user is already unauthorised';
		return;
	end if;

	--------------------------------------------
	-- update authorised
	--
	update application.user
	set
		is_authorised = _is_authorised,
		authorised_date = case when _is_authorised then now() else null end
	where user_id = _user_id;

	--TODO audit authorisation change
	
end;
$$ language plpgsql;