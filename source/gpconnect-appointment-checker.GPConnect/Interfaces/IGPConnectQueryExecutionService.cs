﻿using gpconnect_appointment_checker.DTO.Request.GpConnect;
using gpconnect_appointment_checker.DTO.Response.GpConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using gpconnect_appointment_checker.Helpers.Enumerations;

namespace gpconnect_appointment_checker.GPConnect.Interfaces
{
    public interface IGpConnectQueryExecutionService
    {
        Task<SlotSimple> ExecuteFreeSlotSearch(RequestParameters requestParameters, DateTime startDate, DateTime endDate, string baseAddress);
        List<SlotSimple> ExecuteFreeSlotSearch(List<RequestParametersList> requestParameters, DateTime startDate, DateTime endDate);
        List<SlotEntrySummaryCount> ExecuteFreeSlotSearchSummary(List<RequestParametersList> requestParameters, DateTime startDate, DateTime endDate);
        List<CapabilityStatementList> ExecuteFhirCapabilityStatement(List<RequestParametersList> requestParameters);
        Task<CapabilityStatement> ExecuteFhirCapabilityStatement(RequestParameters requestParameters, string baseAddress);
        void SendToAudit(List<string> auditSearchParameters, List<string> auditSearchIssues, Stopwatch stopWatch, int? resultCount = 0);
    }
}
