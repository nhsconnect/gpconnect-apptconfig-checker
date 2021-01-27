drop function if exists application.get_organisation;

create function application.get_organisation
(
	_ods_code varchar(20)
)
returns table
(
	organisation_id integer,
	ods_code varchar(10),
	organisation_type_name varchar(200),
	organisation_name varchar(100),
	address_line_1 varchar(100),
	address_line_2 varchar(100),
	locality varchar(100),
	city varchar(100),
	county varchar(100),
	postcode varchar(100)
)
as $$
begin

	--------------------------------------------
	-- clean parameters
	--
	_ods_code = trim(upper(coalesce(_ods_code, '')));

	--------------------------------------------
	-- return organisation
	--
	return query
	select
		o.organisation_id,
		o.ods_code,
		ot.organisation_type_name,
		o.organisation_name,
		o.address_line_1,
		o.address_line_2,
		o.locality,
		o.city,
		o.county,
		o.postcode
	from application.organisation o
	inner join application.organisation_type ot on o.organisation_type_id = ot.organisation_type_id
	where o.ods_code = _ods_code;
	
end;
$$ language plpgsql;
