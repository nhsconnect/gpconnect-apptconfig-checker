drop function if exists configuration.get_spine_message_type;

create function configuration.get_spine_message_type
(
)
returns table
(
    spine_message_type_id smallint,
    spine_message_type_name varchar(200),
    interaction_id varchar(200)
)
as $$
begin
	return query
	select
	    smt.spine_message_type_id,
	    smt.spine_message_type_name,
	    smt.interaction_id
	from configuration.spine_message_type smt;	
end;
$$ language plpgsql;
