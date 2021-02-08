select
	client_id,
	client_secret,
	callback_path,
	auth_scheme,
	challenge_scheme,
	auth_endpoint,
	token_endpoint,
	metadata_endpoint,
	signed_out_callback_path
from configuration.get_sso_configuration
(
);
