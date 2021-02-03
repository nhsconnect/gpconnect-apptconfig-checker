namespace gpconnect_appointment_checker.Helpers.Constants
{
    public class SearchConstants
    {
        public const string ISSUEWITHODSCODESLABEL = "Issue with ODS codes";
        public const string ISSUEWITHCONSUMERODSCODELABEL = "Issue with Consumer ODS code";
        public const string ISSUEWITHPROVIDERODSCODELABEL = "Issue with Provider ODS code";
        public const string ISSUEWITHGPCONNECTPROVIDERLABEL = "Issue with GP Connect Provider";
        public const string ISSUEWITHSENDINGMESSAGETOPROVIDERSYSTEMLABEL = "An error occured when sending a message to the provider system";
        public const string ISSUEWITHLDAPLABEL = "An LDAP error has occurred";

        public const string ACCESSLABEL = "Access to GP Connect Appointment Checker";
        public const string ACCESSTEXT = "You must be authorised to use this application. Please contact <a href=\"mailto:{0}\">{0}</a> to request access.";
        public const string ACCESSGRANTEDTEXT = "Once you have been granted access, please click <a href=\"{0}\">here</a> to return to the login page.";
        public const string ACCESSTEXTALT = "This application is restricted to registered users of the NHS. You need to sign in to use it.";

        public const string SIGNEDINTEXT = "Signed in to GP Connect Appointment Checker as {0}";
        public const string RUNASEARCHTEXT = "Run a search";
        public const string SIGNINBUTTONTEXT = "Sign in";
        public const string SIGNOUTBUTTONTEXT = "Sign out";

        public const string ISSUEWITHODSCODESTEXT = "Sorry, but the Provider ODS code '{0}' and the Consumer ODS code '{1}' have not been found";
        public const string ISSUEWITHCONSUMERODSCODETEXT = "Sorry, but the Consumer ODS code '{0}' has not been found";
        public const string ISSUEWITHPROVIDERODSCODETEXT = "Sorry, but the Provider ODS code '{0}' has not been found";
        public const string ISSUEWITHGPCONNECTPROVIDERTEXT = "Sorry, this organisation is not enabled for GP Connect";
        public const string ISSUEWITHGPCONNECTPROVIDERNOTENABLEDTEXT = "Sorry, but the Provider ODS code {0} is not enabled for GP Connect Appointment Management";

        public const string ISSUEWITHSENDINGMESSAGETOPROVIDERSYSTEMTEXT = "The error returned was \"{0} ({1})\".";
        public const string ISSUEWITHLDAPTEXT = "The SDS server is currently unavailable. Please try your search again.";

        public const string ERRORHEADERTITLETEXT = "Error in GP Connect Appointment Checker";
        public const string ERRORTEXT = "Sorry, but it looks like an error has occurred in the application. The error has been logged and will be investigated.";
        public const string RETURNTOHOMEPAGETEXT = "Please click here to return to the Home page.";
        public const string RETURNTOSEARCHPAGETEXT = "Please click here to return to the Search page.";

        public const string ISSUEWITHTIMEOUTTITLETEXT = "Issue with timeout";
        public const string ISSUEWITHTIMEOUTTEXT = "Sorry, the search has timed out before it returned the results. The error has been logged and will be investigated.";

        public const string SEARCHSTATSTEXT = "Search took {0} and completed at {1}<br />{2} free slots found";

        public const string PROVIDERODSCODEREQUIREDERRORMESSAGE = "You must enter a provider ODS code";
        public const string CONSUMERODSCODEREQUIREDERRORMESSAGE = "You must enter a consumer ODS code";

        public const string PROVIDERODSCODEVALIDERRORMESSAGE = "You must enter a valid provider ODS code";
        public const string CONSUMERODSCODEVALIDERRORMESSAGE = "You must enter a valid consumer ODS code";

        public const string SEARCHRESULTSSEARCHATTEXT = "Searching at:";
        public const string SEARCHRESULTSPUBLISHERLABEL = "Publisher:";
        public const string SEARCHRESULTSSEARCHONBEHALFOFTEXT = "Searching on behalf of:";
        public const string SEARCHRESULTSAVAILABLEAPPOINTMENTSLOTSTEXT = "Available Appointment Slots";
        public const string SEARCHRESULTSNOAVAILABLEAPPOINTMENTSLOTSTEXT = "No available appointment slots found";
        public const string SEARCHRESULTSNOADDRESSPROVIDEDTEXT = "(no address provided)";

        public const string SEARCHRESULTSAPPOINTMENTDATECOLUMNHEADERTEXT = "Appointment date";
        public const string SEARCHRESULTSSESSIONNAMECOLUMNHEADERTEXT = "Session name";
        public const string SEARCHRESULTSSTARTTIMECOLUMNHEADERTEXT = "Start time";
        public const string SEARCHRESULTSDURATIONCOLUMNHEADERTEXT = "Duration";
        public const string SEARCHRESULTSSLOTTYPECOLUMNHEADERTEXT = "Slot type";
        public const string SEARCHRESULTSMODEOFAPPOINTMENTCOLUMNHEADERTEXT = "Mode of appointment";
        public const string SEARCHRESULTSPRACTITIONERCOLUMNHEADERTEXT = "Practitioner";

        public const string SEARCHINPUTPROVIDERODSCODELABEL = "Enter a provider ODS code (search at)";
        public const string SEARCHINPUTCONSUMERODSCODELABEL = "Enter a consumer ODS code (search on behalf of)";
        public const string SEARCHINPUTDATERANGELABEL = "Select a date range";
        public const string SEARCHINPUTINCLUDEPASTAPPOINTMENTSLABEL = "Include past appointments (EMIS Health only)";
        public const string SEARCHFORFREESLOTSBUTTONTEXT = "Search for free slots";
        public const string CLEARBUTTONTEXT = "Clear";
    }
}
