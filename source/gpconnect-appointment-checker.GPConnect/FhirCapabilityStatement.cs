using gpconnect_appointment_checker.DTO.Request.GpConnect;
using gpconnect_appointment_checker.DTO.Response.GpConnect;
using gpconnect_appointment_checker.GPConnect.Constants;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.GPConnect
{
    public partial class GpConnectQueryExecutionService
    {
        private async Task<CapabilityStatement> GetCapabilityStatement(RequestParameters requestParameters, string baseAddress)
        {
            try
            {
                var spineMessageType = (_configurationService.GetSpineMessageTypes()).FirstOrDefault(x =>
                    x.SpineMessageTypeId == (int)SpineMessageTypes.GpConnectReadMetaData);
                requestParameters.SpineMessageTypeId = (int)SpineMessageTypes.GpConnectReadMetaData;
                requestParameters.InteractionId = spineMessageType?.InteractionId;

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                _spineMessage.SpineMessageTypeId = requestParameters.SpineMessageTypeId;

                var client = _httpClientFactory.CreateClient("GpConnectClient");
                AddRequiredRequestHeaders(requestParameters, client);
                _spineMessage.RequestHeaders = client.DefaultRequestHeaders.ToString();

                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{AddSecureSpineProxy(baseAddress, requestParameters)}/metadata")
                };

                var response = await client.SendAsync(request);
                var responseStream = await response.Content.ReadAsStringAsync();

                _spineMessage.ResponsePayload = responseStream;
                _spineMessage.ResponseStatus = response.StatusCode.ToString();
                _spineMessage.RequestPayload = request.ToString();
                _spineMessage.ResponseHeaders = response.Headers.ToString();
                stopWatch.Stop();
                _spineMessage.RoundTripTimeMs = stopWatch.ElapsedMilliseconds;
                _logService.AddSpineMessageLog(_spineMessage);

                var results = JsonConvert.DeserializeObject<CapabilityStatement>(responseStream);
                return results;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "An error occurred in trying to execute a GET request");
                throw;
            }
        }
    }
}
