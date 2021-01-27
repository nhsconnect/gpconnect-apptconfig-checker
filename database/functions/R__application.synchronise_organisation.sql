drop function if exists application.synchronise_organisation;

create function application.synchronise_organisation
(
	_ods_code varchar(10),
	_organisation_type_name varchar(200),
	_organisation_name varchar(100),
	_address_line_1 varchar(100),
	_address_line_2 varchar(100),
	_locality varchar(100),
	_city varchar(100),
	_county varchar(100),
	_postcode varchar(100)
)
returns void
as $$
declare
	_organisation_type_id smallint;
begin

	--------------------------------------------
	-- clean parameters
	--
	_ods_code = upper(trim(coalesce(_ods_code, '')));
	_organisation_type_name = trim(coalesce(_organisation_type_name, ''));
	_organisation_name = trim(coalesce(_organisation_name, ''));
	_address_line_1 = trim(coalesce(_address_line_1, ''));
	_address_line_2 = trim(coalesce(_address_line_2, ''));
	_locality = trim(coalesce(_locality, ''));
	_city = trim(coalesce(_city, ''));
	_county = trim(coalesce(_county, ''));
	_postcode = upper(trim(coalesce(_postcode, '')));

	--------------------------------------------
	-- insert/get organisation type
	--
	select 
		ot.organisation_type_id into _organisation_type_id
	from application.organisation_type ot
	where lower(ot.organisation_type_name) = lower(_organisation_type_name);

	if (_organisation_type_id is null)
	then
		insert into application.organisation_type
		(
			organisation_type_id,
			organisation_type_name
		)
		select
			coalesce(max(ot.organisation_type_id), 0) + 1,
			_organisation_type_name
		from application.organisation_type ot
		returning organisation_type_id into _organisation_type_id;
	end if;
	
	--------------------------------------------
	-- insert/get organisation
	--
	if not exists
	(
		select 
			*
		from application.organisation o
		where o.ods_code = _ods_code
	)
	then
		insert into application.organisation
		(
			ods_code,
			organisation_type_id,
			organisation_name,
			address_line_1,
			address_line_2,
			locality,
			city,
			county,
			postcode,
			added_date,
			last_sync_date
		)
		values
		(
			_ods_code,
			_organisation_type_id,
			_organisation_name,
			_address_line_1,
			_address_line_2,
			_locality,
			_city,
			_county,
			_postcode,
			now(),
			now()
		);
	else
		update application.organisation
		set
			organisation_name = _organisation_name,
			organisation_type_id = _organisation_type_id,
			address_line_1 = _address_line_1,
			address_line_2 = _address_line_2,
			locality = _locality,
			city = _city,
			county = _county,
			postcode = _postcode,
			last_sync_date = now()
		where ods_code = _ods_code
		and
		(
			lower(organisation_name) != lower(_organisation_name)
			or organisation_type_id != _organisation_type_id
			or lower(address_line_1) != lower(_address_line_1)
			or lower(address_line_2) != lower(_address_line_2)
			or lower(locality) != lower(_locality)
			or lower(city) != lower(_city)
			or lower(county) != lower(_county)
			or lower(postcode) != lower(_postcode)
		);

		-- TODO write audit based on fields changed

	end if;

end;
$$ language plpgsql;