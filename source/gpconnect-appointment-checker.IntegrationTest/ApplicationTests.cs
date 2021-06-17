using Dapper.FluentMap;
using gpconnect_appointment_checker.DAL;
using gpconnect_appointment_checker.DAL.Application;
using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DAL.Mapping;
using gpconnect_appointment_checker.DTO.Response.Application;
using gpconnect_appointment_checker.Helpers.Enumerations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace gpconnect_appointment_checker.IntegrationTest
{

    [Collection("Sequential")]
    public class ApplicationTests
    {
        private readonly ApplicationService _applicationService;
        private readonly DataService _dataService;

        public ApplicationTests()
        {
            var mockLogger = new Mock<ILogger<ApplicationService>>();
            var mockLoggerDataService = new Mock<ILogger<DataService>>();
            var mockLogService = new Mock<ILogService>();
            var mockAuditService = new Mock<IAuditService>();
            var mockEmailService = new Mock<IEmailService>();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockConfiguration = new Mock<IConfiguration>();

            SetupFluentMappings();
            SetupContextAccessor(mockHttpContextAccessor);
            SetupConfiguration(mockConfiguration);

            _dataService = new DataService(mockConfiguration.Object, mockLoggerDataService.Object);
            _applicationService = new ApplicationService(mockConfiguration.Object, mockLogger.Object, _dataService, mockAuditService.Object, mockLogService.Object, mockHttpContextAccessor.Object, mockEmailService.Object);
        }

        [Theory]
        [InlineData("A20047", "PR", "DR LEGG'S SURGERY", "LS1 4HY")]
        [InlineData("B82617", "PR", "COXWOLD SURGERY", "YO61 4BB")]
        public async void OrganisationFound(string odsCode, string organisationTypeCode, string organisationName, string postalCode)
        {
            var result = _applicationService.GetOrganisation(odsCode);
            Assert.IsType<Organisation>(result);
            Assert.Equal(odsCode, result.ODSCode);
            Assert.Equal(organisationTypeCode, result.OrganisationTypeCode);
            Assert.Equal(organisationName, result.OrganisationName);
            Assert.Equal(postalCode, result.PostalCode);
            Assert.True(result.PostalAddressFields.Length > 0);
        }

        [Theory]
        [InlineData("X00000")]
        [InlineData("Y00000")]
        public async void OrganisationNotFound(string odsCode)
        {
            var result = _applicationService.GetOrganisation(odsCode);
            Assert.Null(result);
        }

        [Theory]
        [InlineData(SortBy.EmailAddress, SortDirection.ASC)]
        [InlineData(SortBy.AccessRequestCount, SortDirection.ASC)]
        [InlineData(SortBy.LastLogonDate, SortDirection.ASC)]
        [InlineData(SortBy.EmailAddress, SortDirection.DESC)]
        [InlineData(SortBy.AccessRequestCount, SortDirection.DESC)]
        [InlineData(SortBy.LastLogonDate, SortDirection.DESC)]
        public async void UsersFound(SortBy sortBy, SortDirection sortDirection)
        {
            var result = _applicationService.GetUsers(sortBy, sortDirection);
            Assert.IsType<List<User>>(result);
            Assert.True(result.Count > 0);
            Assert.Contains(result, x => x.UserId > 0);
            Assert.Contains(result, x => !string.IsNullOrEmpty(x.EmailAddress));
            Assert.Contains(result, x => !string.IsNullOrEmpty(x.DisplayName));
            Assert.Contains(result, x => !string.IsNullOrEmpty(x.OrganisationName));
            Assert.Contains(result, x => x.LastLogonDate <= DateTime.Now);
            Assert.Contains(result, x => x.UserAccountStatusId >= 1);
            Assert.All(result, x => Assert.IsType<bool>(x.IsAdmin));
            Assert.All(result, x => Assert.IsType<bool>(x.IsPastLastLogonThreshold));
            Assert.All(result, x => Assert.IsType<bool>(x.MultiSearchEnabled));
        }

        private static void SetupConfiguration(Mock<IConfiguration> mockConfiguration)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
            var mockConfSection = new Mock<IConfigurationSection>();
            mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "DefaultConnection")]).Returns(connectionString);
            mockConfiguration.Setup(a => a.GetSection(It.Is<string>(s => s == "ConnectionStrings")))
                .Returns(mockConfSection.Object);

            var mockLoggerDataService = new Mock<ILogger<DataService>>();            
            mockConfiguration.Setup(a => a.GetSection(It.Is<string>(s => s == "ConnectionStrings"))).Returns(mockConfSection.Object);
        }

        private static void SetupContextAccessor(Mock<IHttpContextAccessor> mockHttpContextAccessor)
        {
            var context = new DefaultHttpContext();
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);
        }

        private static void SetupFluentMappings()
        {            
            if (FluentMapper.EntityMaps.IsEmpty)
            {
                FluentMapper.Initialize(config =>
                {
                    config.AddMap(new UserMap());
                    config.AddMap(new OrganisationMap());
                    config.AddMap(new SearchResultMap());
                    config.AddMap(new SearchResultByGroupMap());
                    config.AddMap(new SearchGroupMap());
                    config.AddMap(new EmailTemplateMap());
                });
            }
        }
    }
}