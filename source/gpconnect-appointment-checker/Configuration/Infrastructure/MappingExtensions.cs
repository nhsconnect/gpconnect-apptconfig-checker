﻿using Dapper.FluentMap;
using gpconnect_appointment_checker.DAL.Mapping;

namespace gpconnect_appointment_checker.Configuration.Infrastructure
{
    public static class MappingExtensions
    {
        public static void ConfigureMappingServices()
        {
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new SdsQueryMap());
                config.AddMap(new SpineMessageTypeMap());
                config.AddMap(new UserMap());
                config.AddMap(new OrganisationMap());
                config.AddMap(new SearchResultMap());
                config.AddMap(new SearchResultByGroupMap());
                config.AddMap(new SearchGroupMap());
                config.AddMap(new SpineMessageMap());
                config.AddMap(new ReportingMap());
                config.AddMap(new EmailTemplateMap());
            });
        }
    }
}
