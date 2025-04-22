using DevExpress.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        private static readonly Regex regex = new Regex("^[a-zA-Z0-9]");
        public static async Task<Person?> DuplicatePersonAsync(U3ADbContext dbc, Person person, string CurrentMemberID)
        {
            int currentID;
            List<Person>? potentialDuplicates = new List<Person>();
            if (!string.IsNullOrWhiteSpace(CurrentMemberID))
            {
                if (int.TryParse(CurrentMemberID, out currentID))
                {
                    // old member ID test
                    potentialDuplicates = await dbc.Person.
                                            Where(x => x.ConversionID == currentID).
                                            ToListAsync();
                }
            }
            if (!potentialDuplicates.Any())
            {
                // Same Data Import Timestamp???
                potentialDuplicates = await dbc.Person.
                    Where(x => x.DataImportTimestamp == person.DataImportTimestamp).ToListAsync();

            }
            if (potentialDuplicates.Any())
            {
                return potentialDuplicates.First();
            }
            else
            {
                return await DuplicatePersonAsync(dbc, person);
            }
        }
        private static async Task<Person?> DuplicatePersonAsync(U3ADbContext dbc, Person person)
        {
            int matches = 0;
            if (string.IsNullOrWhiteSpace(person.Gender)) return null;
            if (string.IsNullOrWhiteSpace(person.FirstName)) return null;
            if (string.IsNullOrWhiteSpace(person.LastName)) return null;
            List<Person> potentialDuplicates;
            potentialDuplicates = await dbc.Person
                                    .Where(x => x.ID != person.ID &&
                                           x.Gender.ToUpper().Trim() == person.Gender.ToUpper().Trim() &&
                                           x.LastName.ToUpper().Trim() == person.LastName.ToUpper().Trim() &&
                                           x.FirstName.ToUpper().Trim() == person.FirstName.ToUpper().Trim())
                                    .ToListAsync();
            foreach (var p in potentialDuplicates)
            {
                matches = CountMatches(person, p);
                if (matches > 0) { return p; }
            }
            potentialDuplicates = await dbc.Person
                                    .Where(x => x.ID != person.ID &&
                                           x.Gender.ToUpper().Trim() == person.Gender.ToUpper().Trim() &&
                                           x.LastName.ToUpper().Trim() == person.LastName.ToUpper().Trim())
                                    .ToListAsync();
            foreach (var p in potentialDuplicates)
            {
                matches = CountMatches(person, p);
                if (matches > 1) { return p; }
            }
            potentialDuplicates = await dbc.Person
                                    .Where(x => x.ID != person.ID &&
                                           x.Gender.ToUpper().Trim() == person.Gender.ToUpper().Trim() &&
                                           x.FirstName.ToUpper().Trim() == person.FirstName.ToUpper().Trim())
                                    .ToListAsync();
            foreach (var p in potentialDuplicates)
            {
                matches = CountMatches(person, p);
                if (matches > 2) { return p; }
            }
            return null;
        }

        private static int CountMatches(Person person, Person duplicate)
        {
            int count = 0;
            if (person.BirthDate.HasValue || duplicate.BirthDate.HasValue)
            {
                if ((person.BirthDate ?? DateTime.MinValue) != (duplicate.BirthDate ?? DateTime.MinValue)) { return count; }
                else count++;
            }
            if ((!string.IsNullOrWhiteSpace(person.AdjustedMobile)) &&
                                strip(person.AdjustedMobile) == strip(duplicate.AdjustedMobile ?? "")) { count++; }
            if ((!string.IsNullOrWhiteSpace(person.AdjustedICEPhone)) &&
                                strip(person.AdjustedICEPhone) == strip(duplicate.AdjustedICEPhone ?? "")) { count++; }
            if ((!string.IsNullOrWhiteSpace(person.Email)) &&
                                strip(person.Email) == strip(duplicate.Email ?? "")) { count++; }
            if (person.LastName != duplicate.LastName && AreWordsSimilar(person.LastName, duplicate.LastName)) { count++; }
            if (person.FirstName != duplicate.FirstName && AreWordsSimilar(person.FirstName, duplicate.FirstName)) { count++; }
            if (IsAddressSimilar(person.Address, duplicate.Address)) count++;
            return count;
        }

        private static bool IsAddressSimilar(string address1, string address2)
        {
            bool result;
            int distance = LevenshteinDistance(strip(AbbreviateRoadName(address1)),
                                                strip(AbbreviateRoadName(address2)));
            result = (distance <= 1) ? true : false;
            return result;
        }
        private static bool AreWordsSimilar(string word1, string word2)
        {
            bool result;
            int distance = LevenshteinDistance(strip(word1), strip(word2));
            result = (distance == 1) ? true : false;
            return result;
        }
        static int LevenshteinDistance(string s, string t)
        {
            int m = s.Length;
            int n = t.Length;
            int[,] d = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++)
            {
                d[i, 0] = i;
            }
            for (int j = 0; j <= n; j++)
            {
                d[0, j] = j;
            }
            for (int j = 1; j <= n; j++)
            {
                for (int i = 1; i <= m; i++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[m, n];
        }

        private static string AbbreviateRoadName(string roadName)
        {
            var result = roadName;
            Dictionary<string, string> roadNameDictionary = new Dictionary<string, string>()
                {
                    {"Alley", "Aly"},
                    {"Anex", "Anx"},
                    {"Arcade", "Arc"},
                    {"Avenue", "Av"},
                    {"Boulevard", "Blvd"},
                    {"Circle", "Cir"},
                    {"Court", "Ct"},
                    {"Crescent", "Cres"},
                    {"Drive", "Dr"},
                    {"Esplanade", "Esp"},
                    {"Expressway", "Expy"},
                    {"Freeway", "Fwy"},
                    {"Garden", "Gdn"},
                    {"Gardens", "Gdns"},
                    {"Green", "Grn"},
                    {"Highway", "Hwy"},
                    {"Lane", "Ln"},
                    {"Park", "Pk"},
                    {"Parkway", "Pkwy"},
                    {"Path", "Path"},
                    {"Place", "Pl"},
                    {"Plaza", "Plz"},
                    {"Point", "Pt"},
                    {"Road", "Rd"},
                    {"Route", "Rte"},
                    {"Row", "Row"},
                    {"Square", "Sq"},
                    {"Street", "St"},
                    {"Terrace", "Ter"},
                    {"Trail", "Trl"},
                    {"Turnpike", "Tpke"}
                };
            foreach (var kvp in roadNameDictionary)
            {
                if (roadName.Contains(kvp.Key))
                {
                    result = roadName.Replace(kvp.Key, kvp.Value);
                    break;
                }
            }
            return result;
        }

        private static string strip(string s)
        {
            return String.Concat(s.Where(c => !Char.IsWhiteSpace(c) && Char.IsLetterOrDigit(c))).ToUpper();
        }
        private static string[] GetTokens(string s)
        {
            // remove non-alphanumeric
            string cleanText = Regex.Replace(s, "[^0-9a-zA-Z]+", " ");
            List<string> tokens = cleanText.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
            tokens.AddRange(CamelCaseToTokens(tokens));
            List<string> additionalTokens = new();
            // allow for O'Brien, O'Donnell etc
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var token = tokens[i];
                if (token == "O")
                {
                    additionalTokens.Add($"O'{tokens[i + 1]}");
                }
                else if (token.StartsWithInvariantCultureIgnoreCase("O") && token.Length > 1)
                {
                    additionalTokens.Add($"O'{token.Substring(1)}");
                }
            }
            tokens.AddRange(additionalTokens);
            return tokens.ToArray();
        }

        private static string[] CamelCaseToTokens(List<string> tokens)
        {
            List<string> result = new List<string>();
            foreach (var token in tokens)
            {
                if (token.ToUpper() == token || token.ToLower() == token) { continue; }
                // add space if CamelCase
                var normalText = Regex.Replace(token, "(\\B[A-Z])", " $1");
                result.AddRange(normalText.Split(" ", StringSplitOptions.RemoveEmptyEntries));
            }
            return result.ToArray();
        }
        public static async Task<string> GetDuplicateMarkupToContinue(U3ADbContext dbc, Person person)
        {
            var duplicate = await DuplicatePersonAsync(dbc, person);
            if (duplicate != null)
            {
                var result = CreateDuplicateTable(person, duplicate);
                var sb = new StringBuilder();
                sb.AppendLine("<p>A potential duplicate record with the following details has been found in the database.");
                sb.AppendLine("It is strongly recommended you <strong>Cancel</strong> this entry and investigate further...</p>");
                result.Insert(0, sb.ToString());
                return result.ToString();
            }
            return null;
        }
        public static async Task<string> GetDuplicateMarkupToProhibit(U3ADbContext dbc, Person person)
        {
            var duplicate = await DuplicatePersonAsync(dbc, person);
            if (duplicate != null)
            {
                var result = CreateDuplicateTable(person, duplicate);
                var sb = new StringBuilder();
                sb.AppendLine("<p>The record cannot be saved because a potential duplicate record with the following details has been found in the database.");
                sb.AppendLine("Please contact your U3A to investigate further.</p>");
                result.Insert(0, sb.ToString());
                return result.ToString();
            }
            return null;
        }

        private static StringBuilder CreateDuplicateTable(Person person, Person duplicate)
        {
            var result = new StringBuilder();
            string birthDate = (duplicate.BirthDate.HasValue) ? duplicate.BirthDate.Value.ToString("d") : "unknown";
            string email = (!string.IsNullOrWhiteSpace(duplicate.Email)) ? duplicate.Email : "unknown";
            string mobile = (!string.IsNullOrWhiteSpace(duplicate.Mobile)) ? duplicate.Mobile : "unknown";
            string homePhone = (!string.IsNullOrWhiteSpace(duplicate.HomePhone)) ? duplicate.HomePhone : "unknown";
            string ICEPhone = (!string.IsNullOrWhiteSpace(duplicate.ICEPhone)) ? duplicate.ICEPhone : "unknown";
            result.AppendLine("<table class='table'>");
            result.AppendLine("<thead><tr>");
            result.AppendLine("<th scope='col'>Field</th>");
            result.AppendLine("<th scope='col'>Value</th>");
            result.AppendLine("<th scope='col'>Same?</th>");
            result.AppendLine("</tr></thead>");
            result.AppendLine("<tbody>");
            result.AppendLine($"<tr><td>First Name:</td><td>{duplicate.FirstName}</td><td>{CheckSameFirstName(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>Last Name:</td><td>{duplicate.LastName}</td><td>{CheckSameLastName(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>Gender:</td><td>{duplicate.Gender}</td><td>{CheckSameGender(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>Address</td><td>{duplicate.Address}</td><td>{CheckSameAddress(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>Birth Date:</td><td>{birthDate}</td><td>{CheckSameBirthdate(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>Mobile:</td><td>{mobile}</td><td>{CheckSameMobile(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>Email:</td><td>{email}</td><td>{CheckSameEmail(person, duplicate)}</td></tr>");
            result.AppendLine($"<tr><td>ICE Phone:</td><td>{ICEPhone}</td><td>{CheckSameICEPhone(person, duplicate)}</td></tr>");
            result.AppendLine("</tbody></table>");
            return result;
        }

        const string CHECK_MARK = "<i class='bi bi-arrow-left'></i>";
        static string CheckSameMobile(Person person, Person duplicate)
        {
            return (person.AdjustedMobile != null && strip(person.AdjustedMobile) == strip(duplicate.AdjustedMobile ?? "")) ? CHECK_MARK : string.Empty;
        }
        static string CheckSameICEPhone(Person person, Person duplicate)
        {
            return (person.AdjustedICEPhone != null && strip(person.AdjustedICEPhone) == strip(duplicate.AdjustedHomePhone ?? "")) ? CHECK_MARK : string.Empty;
        }
        static string CheckSameEmail(Person person, Person duplicate)
        {
            return (person.Email != null && strip(person.Email) == strip(duplicate.Email ?? "")) ? CHECK_MARK : string.Empty;
        }
        static string CheckSameBirthdate(Person person, Person duplicate)
        {
            if (person.BirthDate.HasValue && duplicate.BirthDate.HasValue)
            {
                return (person.BirthDate.Value == duplicate.BirthDate.Value) ? CHECK_MARK : string.Empty;
            }
            return string.Empty;
        }
        static string CheckSameFirstName(Person person, Person duplicate)
        {
            var result = (person.FirstName != null && strip(person.FirstName) == strip(duplicate.FirstName ?? "")) ? CHECK_MARK : string.Empty;
            if (AreWordsSimilar(person.FirstName, duplicate.FirstName))
            {
                var ld = LevenshteinDistance(strip(person.FirstName), strip(duplicate.FirstName));
                if (ld > 0) { result = ld.ToString(); }
            }
            return result;
        }
        static string CheckSameLastName(Person person, Person duplicate)
        {
            var result = (person.LastName != null && strip(person.LastName) == strip(duplicate.LastName ?? "")) ? CHECK_MARK : string.Empty;
            if (AreWordsSimilar(person.LastName, duplicate.LastName))
            {
                var ld = LevenshteinDistance(strip(person.LastName), strip(duplicate.LastName));
                if (ld > 0) { result = ld.ToString(); }
            }
            return result;
        }
        static string CheckSameAddress(Person person, Person duplicate)
        {
            var result = (person.Address != null && strip((AbbreviateRoadName(person.Address))) == strip((AbbreviateRoadName(duplicate.Address ?? "")))) ? CHECK_MARK : string.Empty;
            if (IsAddressSimilar(person.Address, duplicate.Address))
            {
                var ld = LevenshteinDistance(strip(AbbreviateRoadName(person.Address)),
                                                strip(AbbreviateRoadName(duplicate.Address)));
                if (ld > 0) { result = ld.ToString(); }
            }
            return result;
        }
        static string CheckSameGender(Person person, Person duplicate)
        {
            return (person.Gender != null && strip(person.Gender) == strip(duplicate.Gender ?? "")) ? CHECK_MARK : string.Empty;
        }

        public static BindingList<Person> ReportableDuplicatePersons(U3ADbContext dbc)
        {
            BindingList<Person> result = new BindingList<Person>();
            List<Person> persons;
            List<DuplicatePerson> duplicates = new List<DuplicatePerson>();
            persons = dbc.Person.AsNoTracking().ToList();

            foreach (var p in persons)
            {
                var d = duplicates.Where(x => strip(x.LastName) == strip(p.LastName) &&
                                        strip(x.FirstName) == strip(p.FirstName)).FirstOrDefault();
                if (d == null)
                {
                    d = new DuplicatePerson() { FirstName = p.FirstName, LastName = p.LastName };
                    duplicates.Add(d);
                }
                d.Persons.Add(p);
            }
            foreach (var d in duplicates.Where(x => x.Persons.Count > 1))
            {
                foreach (var p in d.Persons)
                {
                    result.Add(p);
                }
            }
            return result;
        }


        static bool AreAddressesAtSameLocation(string address1, string address2)
        {


            HttpClient client = new HttpClient();
            string baseUrl = "https://maps.googleapis.com/maps/api/geocode/json?";
            string url1 = baseUrl + "address=" + Uri.EscapeDataString(address1) + "&key=YOUR_API_KEY";
            string url2 = baseUrl + "address=" + Uri.EscapeDataString(address2) + "&key=YOUR_API_KEY";
            JToken result1 = GetGeocodeResult(client, url1);
            JToken result2 = GetGeocodeResult(client, url2);
            if (result1 == null || result2 == null)
            {
                return false;
            }
            double lat1 = result1["geometry"]["location"]["lat"].Value<double>();
            double lng1 = result1["geometry"]["location"]["lng"].Value<double>();
            double lat2 = result2["geometry"]["location"]["lat"].Value<double>();
            double lng2 = result2["geometry"]["location"]["lng"].Value<double>();
            return Math.Abs(lat1 - lat2) < 0.0001 && Math.Abs(lng1 - lng2) < 0.0001;


            static JToken GetGeocodeResult(HttpClient client, string url)
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                string json = response.Content.ReadAsStringAsync().Result;
                JObject result = JObject.Parse(json);
                if (!result["status"].Value<string>().Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                return result["results"][0];
            }
        }
    }
}
