namespace gpconnect_appointment_checker.DTO.Response.Configuration
{
    public class Sso
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string callback_path { get; set; }
        public string auth_scheme { get; set; }
        public string auth_endpoint { get; set; }
        public string token_endpoint { get; set; }
        public string challenge_scheme { get; set; }
        public string metadata_endpoint { get; set; }
        public string signed_out_callback_path { get; set; }
    }
}
