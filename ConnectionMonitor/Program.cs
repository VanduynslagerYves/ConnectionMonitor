using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace NetworkTrafficMonitor
{
    class Program
    {
        private static readonly List<string> _filterIpsList = new()
        {
            "127.0.0.1",
            "192.168.1.1",
            "::1",
            string.Empty,
        };

        private static Regex _ipAddressRegex = new Regex(@"\d+\.\d+\.\d+\.\d+", RegexOptions.Compiled);

        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Fetching active connections...");
                var connections = GetActiveConnections();
                if (connections.Count == 0)
                {
                    Console.WriteLine("No active connections found.");
                    return;
                }

                Console.WriteLine("Active connections and their details:");

                foreach (var ip in connections)
                {
                    if (_filterIpsList.Contains(ip)) continue;

                    var ipInfo = await GetIpInfo(ip);
                    if (ipInfo == null) continue;

                    var isLocalHost = IsInvalidIp(ipInfo["ip"]?.ToString() ?? string.Empty);
                    if (isLocalHost) continue;

                    DisplayIpInfo(ipInfo);
                }

                Console.WriteLine("Press a key to refresh");
                Console.ReadLine();
            }

        }

        static bool IsInvalidIp(string ip)
        {
            return _filterIpsList.Contains(ip);
        }

        static List<string> GetActiveConnections()
        {
            List<string> remoteIps = new List<string>();

            ProcessStartInfo psi = new ProcessStartInfo("netstat", "-n")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains("ESTABLISHED"))
                    {
                        MatchCollection matches = _ipAddressRegex.Matches(line);
                        if (matches.Count > 1)
                        {
                            remoteIps.Add(matches[1].Value); // Second IP is the remote one
                        }
                    }
                }
            }

            return remoteIps;
        }

        static async Task<JObject?> GetIpInfo(string ip)
        {
            try
            {
                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync($"https://ipinfo.io/{ip}/json");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data for IP {ip}: {ex.Message}");
            }

            return null;
        }

        static void DisplayIpInfo(JObject ipInfo)
        {
            if (ipInfo == null) return;

            string ip = ipInfo["ip"]?.ToString() ?? "Unknown";
            string country = ipInfo["country"]?.ToString() ?? "Unknown";
            string org = ipInfo["org"]?.ToString() ?? "Unknown";
            string hostname = ipInfo["hostname"]?.ToString() ?? "Unknown";
            string city = ipInfo["city"]?.ToString() ?? "Unknown";
            string region = ipInfo["region"]?.ToString() ?? "Unknown";
            string postal = ipInfo["postal"]?.ToString() ?? "Unknown";
            string timezone = ipInfo["timezone"]?.ToString() ?? "Unknown";
            string loc = ipInfo["loc"]?.ToString() ?? "Unknown";

            Console.WriteLine($"IP: {ip}");
            Console.WriteLine($"  Country: {country}");
            Console.WriteLine($"  Organization: {org}");
            Console.WriteLine($"  Hostname: {hostname}");
            Console.WriteLine($"  City: {city}");
            Console.WriteLine($"  Region: {region}");
            Console.WriteLine($"  Postal Code: {postal}");
            Console.WriteLine($"  Timezone: {timezone}");
            Console.WriteLine($"  Location: {loc}");
            Console.WriteLine($"-----");
        }
    }
}

