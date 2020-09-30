﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace gpconnect_appointment_checker.DTO.Request.GpConnect
{
    public class BaseRequest
    {
        [JsonProperty("resourceType")] 
        public string resourceType { get; set; }
        [JsonProperty("identifier")] 
        public List<Identifier> identifier { get; set; }
    }

    public class Identifier
    {
        [JsonProperty("system")] 
        public string system { get; set; }
        [JsonProperty("value")] 
        public string value { get; set; }
    }
}
