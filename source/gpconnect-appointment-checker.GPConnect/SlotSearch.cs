using gpconnect_appointment_checker.DTO.Request.Audit;
using gpconnect_appointment_checker.DTO.Request.GpConnect;
using gpconnect_appointment_checker.DTO.Response.GpConnect;
using gpconnect_appointment_checker.GPConnect.Constants;
using gpconnect_appointment_checker.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace gpconnect_appointment_checker.GPConnect
{
    public partial class GpConnectQueryExecutionService
    {
        public async Task<SlotSimple> GetFreeSlots(RequestParameters requestParameters, DateTime startDate, DateTime endDate, string baseAddress, bool includePastSlots)
        {
            try
            {
                var spineMessageType = (_configurationService.GetSpineMessageTypes()).FirstOrDefault(x =>
                    x.SpineMessageTypeId == (int)SpineMessageTypes.GpConnectSearchFreeSlots);
                requestParameters.SpineMessageTypeId = (int)SpineMessageTypes.GpConnectSearchFreeSlots;
                requestParameters.InteractionId = spineMessageType?.InteractionId;

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                _spineMessage.SpineMessageTypeId = requestParameters.SpineMessageTypeId;

                var client = _httpClientFactory.CreateClient("GpConnectClient");

                client.Timeout = new TimeSpan(0, 0, 30);
                AddRequiredRequestHeaders(requestParameters, client);
                _spineMessage.RequestHeaders = client.DefaultRequestHeaders.ToString();
                var requestUri = new Uri($"{AddSecureSpineProxy(baseAddress, requestParameters)}/Slot");
                var uriBuilder = AddQueryParameters(requestParameters, startDate, endDate, requestUri, includePastSlots);
                var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                var response = await client.SendAsync(request);
                var responseStream = await response.Content.ReadAsStringAsync();
                
                _spineMessage.ResponsePayload = responseStream;
                _spineMessage.ResponseStatus = response.StatusCode.ToString();
                _spineMessage.RequestPayload = request.ToString();
                _spineMessage.ResponseHeaders = response.Headers.ToString();

                stopWatch.Stop();
                _spineMessage.RoundTripTimeMs = stopWatch.ElapsedMilliseconds;
                _logService.AddSpineMessageLog(_spineMessage);

                var slotSimple = new SlotSimple();
                var results = JsonConvert.DeserializeObject<Bundle>(responseStream);

                if (results.Issue?.Count > 0)
                {
                    slotSimple.Issue = results.Issue;
                    return slotSimple;
                }

                return PopulateSlotSimple(slotSimple, results);
            }
            catch (TimeoutException timeoutException)
            {
                _logger.LogError(timeoutException, "A timeout error has occurred");
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "An error occurred in trying to execute a GET request");
                throw;
            }
        }

        private SlotSimple PopulateSlotSimple(SlotSimple slotSimple, Bundle results)
        {
            slotSimple.SlotEntrySimple = new List<SlotEntrySimple>();

            var slotResources = results.entry?.Where(x => x.resource.resourceType == ResourceTypes.Slot).ToList();
            if (slotResources == null || slotResources?.Count == 0) return slotSimple;

            var practitionerResources =
                results.entry?.Where(x => x.resource.resourceType == ResourceTypes.Practitioner).ToList();
            var locationResources = results.entry?.Where(x => x.resource.resourceType == ResourceTypes.Location).ToList();
            var scheduleResources = results.entry?.Where(x => x.resource.resourceType == ResourceTypes.Schedule).ToList();

            var slotList = (from slot in slotResources?.Where(s => s.resource != null)
                    let practitioner = GetPractitionerDetails(slot.resource.schedule.reference, scheduleResources,
                        practitionerResources)
                    let location = GetLocation(slot.resource.schedule.reference, scheduleResources, locationResources)
                    let schedule = GetSchedule(slot.resource.schedule.reference, scheduleResources)
                    select new SlotEntrySimple
                    {
                        AppointmentDate = slot.resource.start,
                        SessionName = schedule.resource.serviceCategory?.text,
                        StartTime = slot.resource.start,
                        Duration = slot.resource.start.DurationBetweenTwoDates(slot.resource.end),
                        SlotType = slot.resource.serviceType.FirstOrDefault()?.text,
                        DeliveryChannel = slot.resource.extension?.FirstOrDefault()?.valueCode,
                        PractitionerGivenName = practitioner?.name?.FirstOrDefault()?.given?.FirstOrDefault(),
                        PractitionerFamilyName = practitioner?.name?.FirstOrDefault()?.family,
                        PractitionerPrefix = practitioner?.name?.FirstOrDefault()?.prefix?.FirstOrDefault(),
                        PractitionerRole = schedule.resource.extension?.FirstOrDefault()?.valueCodeableConcept?.coding
                            ?.FirstOrDefault()?.display,
                        PractitionerGender = practitioner?.gender,
                        LocationName = location?.name,
                        LocationAddressLines = location?.address?.line,
                        LocationCity = location?.address?.city,
                        LocationCountry = location?.address?.country,
                        LocationDistrict = location?.address?.district,
                        LocationPostalCode = location?.address?.postalCode
                    }).OrderBy(z => z.LocationName)
                .ThenBy(s => s.AppointmentDate)
                .ThenBy(s => s.StartTime);
            slotSimple.SlotEntrySimple.AddRange(slotList);
            return slotSimple;
        }

        private Practitioner GetPractitionerDetails(string reference, List<RootEntry> scheduleResources, List<RootEntry> practitionerResources)
        {
            var schedule = GetSchedule(reference, scheduleResources);
            var schedulePractitioner = schedule?.resource.actor?.FirstOrDefault(x => x.reference.Contains("Practitioner/"));
            var practitionerRootEntry = practitionerResources?.FirstOrDefault(x => schedulePractitioner?.reference == $"Practitioner/{x.resource.id}")?.resource;
            var practitioner = new Practitioner
            {
                gender = practitionerRootEntry?.gender,
                name = JsonConvert.DeserializeObject<List<PractitionerName>>(practitionerRootEntry?.name.ToString())
            };
            return practitioner;
        }

        private Location GetLocation(string reference, List<RootEntry> scheduleResources, List<RootEntry> locationResources)
        {
            var schedule = GetSchedule(reference, scheduleResources);
            var scheduleLocation = schedule?.resource.actor?.FirstOrDefault(x => x.reference.Contains("Location/"));
            var locationRootEntry = locationResources?.FirstOrDefault(x => scheduleLocation?.reference == $"Location/{x.resource.id}")?.resource;
            var location = new Location
            {
                name = locationRootEntry?.name.ToString(),
                address = locationRootEntry?.address != null ? JsonConvert.DeserializeObject<LocationAddress>(locationRootEntry.address.ToString()) : null
            };
            return location;
        }

        private RootEntry GetSchedule(string reference, List<RootEntry> scheduleResources)
        {
            var schedule = scheduleResources.FirstOrDefault(x => reference == $"Schedule/{x.resource.id}");
            return schedule;
        }

        private static UriBuilder AddQueryParameters(RequestParameters requestParameters, DateTime startDate, DateTime endDate, Uri requestUri, bool includePastSlots)
        {
            startDate = RecalculateStartDate(startDate, includePastSlots);
            var uriBuilder = new UriBuilder(requestUri.ToString());
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query.Add(Uri.EscapeDataString("status"), "free");
            query.Add(Uri.EscapeDataString("_include"), "Slot:schedule");
            query.Add(Uri.EscapeDataString("_include:recurse"), "Schedule:actor:Practitioner");
            query.Add(Uri.EscapeDataString("_include:recurse"), "Schedule:actor:Location");
            query.Add(Uri.EscapeDataString("_include:recurse"), "Location:managingOrganization");
            query.Add(Uri.EscapeDataString("start"), $"ge{startDate:yyyy-MM-ddThh:mm:sszzzz}");
            query.Add(Uri.EscapeDataString("end"), $"le{endDate:yyyy-MM-ddThh:mm:sszzzz}");
            query.Add(Uri.EscapeDataString("searchFilter"), $"https://fhir.nhs.uk/Id/ods-organization-code|{requestParameters.ConsumerODSCode}");
            uriBuilder.Query = query.ToString();
            return uriBuilder;
        }

        private static DateTime RecalculateStartDate(DateTime startDate, bool includePastSlots)
        {
            if (DateTime.Now > startDate && !includePastSlots)
            {
                return DateTime.Now;
            }
            return startDate;
        }

        public void SendToAudit(List<string> auditSearchParameters, List<string> auditSearchIssues, Stopwatch stopWatch, int? resultCount = 0)
        {
            var auditEntry = new Entry
            {
                Item1 = auditSearchParameters[0],
                Item2 = auditSearchParameters[1],
                Item3 = auditSearchParameters[2].Replace(":", " "),
                Details = (auditSearchIssues != null && auditSearchIssues.Count > 0) ? string.Join((char)10, auditSearchIssues) : $"{resultCount} free slot(s) returned",
                EntryElapsedMs = Convert.ToInt32(stopWatch.ElapsedMilliseconds),
                EntryTypeId = (int)AuditEntryTypes.SlotSearch
            };
            _auditService.AddEntry(auditEntry);
        }
    }
}
