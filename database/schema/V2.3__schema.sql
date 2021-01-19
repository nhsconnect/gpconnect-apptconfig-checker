alter table configuration.sso drop column if exists endsession_endpoint;

alter table configuration.sso add column signed_out_callback_path varchar(1000) null;
update configuration.sso set signed_out_callback_path = '/auth/externallogout';
alter table configuration.sso alter column signed_out_callback_path set not null;

alter table configuration.sso add constraint configuration_sso_signedoutcallbackpath_ck check (char_length(trim(signed_out_callback_path)) > 0);