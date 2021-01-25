using Newtonsoft.Json;

namespace gpconnect_appointment_checker.DTO.Response.Infrastructure
{
    public class Container
    {
        [JsonProperty("Image")]
        public string Image { get; set; }
    }
}
