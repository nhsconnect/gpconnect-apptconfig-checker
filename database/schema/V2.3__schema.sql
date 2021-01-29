update configuration.sds_query 
set query_attributes = 'nhsMHSEndPoint,nhsMHSPartyKey,uniqueIdentifier' 
where query_name='GetGpProviderEndpointAndPartyKeyByOdsCode';

update configuration.sds_query 
set query_attributes = 'nhsMHSPartyKey,uniqueIdentifier,nhsProductName' 
where query_name='GetGpProviderAsIdByOdsCodeAndPartyKey';
