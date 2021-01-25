using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using gpconnect_appointment_checker.Helpers.Constants;

namespace gpconnect_appointment_checker.Helpers
{
    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input, bool restToLower = false) =>
            input switch
            {
                null => string.Empty,
                "" => string.Empty,
                _ => input.First().ToString().ToUpper() + (restToLower ? input.Substring(1).ToLower() : input.Substring(1))
            };

        public static string Coalesce(params string[] strings)
        {
            return strings.FirstOrDefault(s => !string.IsNullOrEmpty(s));
        }

        public static string StripNonAlphanumericCharacters(this string input)
        {
            var regex = new Regex(ValidationConstants.ALPHANUMERICCHARACTERSONLY);
            return regex.Replace(input, string.Empty).Trim();
        }

        public static string AddressBuilder(List<string> addressLines, string district, string city, string postalCode, string country)
        {
            addressLines ??= new List<string>();
            addressLines.Add(district);
            addressLines.Add(city);
            addressLines.Add(postalCode);
            addressLines.Add(country);
            return string.Join(", ", addressLines.Where(s => !string.IsNullOrEmpty(s)));
        }

        public static string AddressBuilder(List<string> addressLines, string postalCode)
        {
            addressLines ??= new List<string>();
            addressLines.Add(postalCode);
            return string.Join(", ", addressLines.Where(s => !string.IsNullOrEmpty(s.Trim())));
        }
    }
}
