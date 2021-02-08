select
    product_name,
    product_version,
    max_num_weeks_search,
    log_retention_days,
    get_access_email_address
from configuration.get_general_configuration
(
);
