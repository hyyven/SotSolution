using System.Text.Json;
using System;
using System.Management;
using ProxyInterface;
using System.Net.Http;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace LoaderV2
{
    public class AuthClient
    {
        private const string auth_file_path = "C:\\SotSolution\\authBanEvade.json";

        private readonly HttpClient client;

        public AuthClient()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "XXXXXXXXXXXXXXXXXXXX");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        private static string ft_get_pros_id()
        {
            string result = string.Empty;

            try
            {
                using (var searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        result = obj["ProcessorId"]?.ToString();
                        Console.WriteLine("Pros ID: " + result);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur Pros ID: " + ex.Message);
            }
            return result;
        }

        private static string ft_get_bios_serial()
        {
            string result = string.Empty;

            try
            {
                using (var searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BIOS"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        result = obj["SerialNumber"]?.ToString();
                        Console.WriteLine("BIOS Serial: " + result);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur BIOS: " + ex.Message);
            }
            return result;
        }

        private static string ft_get_volume_serial()
        {
            string volume_serial = string.Empty;

            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).Take(1);
                foreach (var drive in drives)
                {
                    using (var searcher = new ManagementObjectSearcher($"select VolumeSerialNumber from Win32_LogicalDisk where DeviceID = '{drive.Name.TrimEnd('\\')}'"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            volume_serial = obj["VolumeSerialNumber"]?.ToString();
                            Console.WriteLine("Numéro de série du volume : " + volume_serial);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la récupération du numéro de série du volume : " + ex.Message);
            }
            return volume_serial;
        }

        private Dictionary<string, string> ft_load_credentials()
        {
            if (File.Exists(auth_file_path))
            {

                string jsonContent = File.ReadAllText(auth_file_path);
                var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                if (credentials != null && credentials.ContainsKey("username") && credentials.ContainsKey("password"))
                {
                    return credentials;
                }
            }
            return null;
        }
        public async Task<HttpResponseMessage> ft_make_request(string url)
        {
            var credentials = ft_load_credentials();
            if (credentials == null)
                return null;

            var fingerprint = new List<string>
            {
                ft_get_pros_id(),
                ft_get_bios_serial(),
                ft_get_volume_serial()
            };

            string hwid_json = JsonSerializer.Serialize(fingerprint);

            if (client.DefaultRequestHeaders.Contains("x-hwid"))
                client.DefaultRequestHeaders.Remove("x-hwid");

            client.DefaultRequestHeaders.Add("x-hwid", hwid_json);

            var contenu = new StringContent(
                JsonSerializer.Serialize(new
                {
                    username = credentials["username"],
                    password = credentials["password"]
                }),
                Encoding.UTF8,
                "application/json"
            );

            return await client.PostAsync(url, contenu);
        }

        public async Task<(HttpResponseMessage response, string username)> ft_auth_login()
        {
            try
            {
                var response = await ft_make_request("http://XX.XXX.XXX.XX:XXXX/login");

                if (response == null)
                {
                    MessageBox.Show("License check failed. No response from server.", "License Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("License check failed. Access denied.", "License Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
                var credentials = ft_load_credentials();
                string username = credentials["username"];
                return (response, username);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An unexpected error occurred during login.\n" +
                    "Please check your internet connection or contact support on Discord if the issue persists.",
                    "Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Environment.Exit(1);
                return (null!, default!);
            }
        }
    }
}