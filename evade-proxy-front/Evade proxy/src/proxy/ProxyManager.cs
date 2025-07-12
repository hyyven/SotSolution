using System.Net;
using System.Security.Cryptography.X509Certificates;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Text.Json;
using ProxyInterface;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Asn1.Cms;
using Evade_proxy.src.proxy;
using LoaderV2;

public class ProxyManager
{
    private static readonly ProxyServer proxy_server = new ProxyServer();
    private static readonly HttpClient http_client = new HttpClient();
    private static readonly AuthClient auth_client = new AuthClient();
    private static readonly CancellationTokenSource cancellation_token_source = new CancellationTokenSource();
    private static X509Certificate2 existing_cert = null;
    private static bool is_cert_installed = false;
    public static bool evadeEnabled = true;
    private static bool ft_check_cert()
    {
        StoreLocation[] stores;
        X509Store store;
        X509Certificate2Collection all_certificates;
        List<X509Certificate2> proxy_certificates;
        List<X509Certificate2> valid_certificates;

        try
        {
            stores = new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine };
            foreach (var location in stores)
            {
                using (store = new X509Store(StoreName.Root, location))
                {
                    store.Open(OpenFlags.ReadOnly);
                    all_certificates = store.Certificates;
                    proxy_certificates = all_certificates.Cast<X509Certificate2>()
                        .Where(cert =>
                            cert.Subject.Contains("Titanium") ||
                            cert.Issuer.Contains("Titanium") ||
                            cert.Subject.Contains("Proxy") ||
                            cert.Issuer.Contains("Proxy"))
                        .ToList();
                    if (proxy_certificates.Any())
                    {
                        valid_certificates = proxy_certificates
                            .Where(cert =>
                                cert.NotBefore <= DateTime.Now &&
                                cert.NotAfter >= DateTime.Now &&
                                cert.HasPrivateKey)
                            .ToList();
                        if (valid_certificates.Any())
                        {
                            existing_cert = valid_certificates.First();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error checking certificate: {exception.Message}");
            return false;
        }
    }

    private static void ft_check_cert_wrapper()
    {
        is_cert_installed = ft_check_cert();
        if (is_cert_installed && existing_cert != null)
        {
            Console.WriteLine("Using existing Titanium Proxy certificate.");
            proxy_server.CertificateManager.RootCertificate = existing_cert;
        }
        else
        {
            Console.WriteLine("Generating new Titanium Proxy certificate...");
            proxy_server.CertificateManager.CreateRootCertificate(false);
            proxy_server.CertificateManager.TrustRootCertificate(true);
        }
    }

    public static void ft_enable_proxy_win_settings(string proxy_address)
    {
        RegistryKey registry;

        try
        {
            using (registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
            {
                if (registry != null)
                {
                    registry.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                    registry.SetValue("ProxyServer", proxy_address, RegistryValueKind.String);
                    Console.WriteLine("Windows proxy settings activated successfully.");
                }
                else
                {
                    Console.WriteLine("Could not access registry key for proxy settings.");
                }
            }
            ft_refresh_internet_settings();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error setting Windows proxy: {exception.Message}");
        }
    }

    public static void ft_disable_proxy_win_settings()
    {
        RegistryKey registry;

        try
        {
            using (registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
            {
                if (registry != null)
                {
                    registry.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    Console.WriteLine("Windows proxy settings disabled successfully.");
                }
            }
            ft_refresh_internet_settings();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error disabling Windows proxy: {exception.Message}");
        }
    }

    private static void ft_refresh_internet_settings()
    {
        [DllImport("wininet.dll")]
        static extern bool InternetSetOption(IntPtr h_internet, int dw_option, IntPtr lp_buffer, int dw_buffer_length);

        // INTERNET_OPTION_SETTINGS_CHANGED = 39
        // INTERNET_OPTION_REFRESH = 37
        InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
    }

    public async Task ft_setup_proxy()
    {
        ExplicitProxyEndPoint explicit_end_point;

        try
        {
            ft_check_cert_wrapper();
            explicit_end_point = new ExplicitProxyEndPoint(IPAddress.Loopback, 8000, true);
            explicit_end_point.BeforeTunnelConnectRequest += ft_on_before_tunnel_connect_request;
            proxy_server.AddEndPoint(explicit_end_point);
            proxy_server.BeforeRequest += ft_on_request;
            proxy_server.BeforeResponse += ft_on_response;

            SystemEvents.SessionEnding += (sender, e) =>
            {
                // hamdoula ca marche
                Console.WriteLine("System shutdown/logoff detected, disabling proxy...");
                ProxySetup.ft_exit().GetAwaiter().GetResult();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Console.WriteLine("Window closing detected, disabling proxy...");
                ProxySetup.ft_exit().GetAwaiter().GetResult();
            };

            await Task.Run(() => proxy_server.Start());
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error setting up proxy: {exception.Message}");
            Environment.Exit(1);
        }
    }

    public async Task ft_stop_proxy()
    {
        cancellation_token_source.Cancel();
        proxy_server.BeforeRequest -= ft_on_request;
        proxy_server.BeforeResponse -= ft_on_response;
        proxy_server.Stop();
        proxy_server.Dispose();
        await Task.CompletedTask;
    }

    private static Task ft_on_before_tunnel_connect_request(object sender, TunnelConnectSessionEventArgs tunnel_event)
    {
        string hostname;

        hostname = tunnel_event.HttpClient.Request.RequestUri.Host;
        if (hostname.Contains("playfabapi.com") || hostname.Contains("privacy.xboxlive.com") || hostname.Contains("sessiondirectory.xboxlive.com"))
        {
            tunnel_event.DecryptSsl = true;
            Console.WriteLine($"SSL decryption for: {hostname}");
        }
        else
        {
            tunnel_event.DecryptSsl = false;
        }
        return Task.CompletedTask;
    }

    private async Task ft_on_request(object sender, SessionEventArgs session_event)
    {
        if (evadeEnabled)
        {
            var request = session_event.HttpClient.Request;
            var absolutePath = request.RequestUri.AbsolutePath;

            if (absolutePath.Contains("/login/api/token/client"))
            {
                if (request.Headers.GetFirstHeader("traceparent") != null)
                {
                    request.Headers.RemoveHeader("traceparent");
                }

                var existing_rare_data = request.Headers.GetFirstHeader("X-Rare-Data");

                if (existing_rare_data != null)
                {
                    try
                    {
                        var rare_data = await ft_get_rare_data();
                        if (rare_data != null)
                        {
                            request.Headers.RemoveHeader("X-Rare-Data");
                            request.Headers.AddHeader("X-Rare-Data", rare_data);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting rare data: {ex.Message}");
                    }
                }
            }
        }

        string fullUrl = session_event.HttpClient.Request.Url;

        if (fullUrl.Contains("/permission/validate"))
        {
            ft_process_xuid(session_event);
        }

        if (fullUrl.Contains("https://e5ed.playfabapi.com/Event/WriteTelemetryEvents"))
        {
            ft_process_new_server(session_event);
        }
    }

    private async Task<string> ft_get_rare_data()
    {
        try
        {
            var response = await auth_client.ft_make_request("http://XX.XXX.XXX.XX:XXXX/token");

            if (response.IsSuccessStatusCode)
            {
                var json_response = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<RareResponse>(json_response);

                if (data?.Data != null)
                {
                    return CryptoManager.DecryptAes(data.Data);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetRareData: {ex.Message}");
            return null;
        }
    }


    private static Task ft_on_response(object sender, SessionEventArgs session_event)
    {
        if (evadeEnabled)
        {
            var response = session_event.HttpClient.Response;
            var requestUriString = session_event.HttpClient.Request.RequestUri.ToString();
        }

        string fullUrl = session_event.HttpClient.Request.Url;

        if (fullUrl.Contains("/sessionTemplates/"))
        {
            ft_process_server_info(session_event);
            Console.WriteLine($"Response: {session_event.HttpClient.Response.StatusCode} {fullUrl}");
        }

        return Task.CompletedTask;
    }


    private static async void ft_process_server_info(SessionEventArgs session_event)
    {
        string response_body;
        Dictionary<string, object> json_data;
        System.Text.Json.JsonElement? properties_element;
        System.Text.Json.JsonElement system_element;
        System.Text.Json.JsonElement matchmaking_element;
        System.Text.Json.JsonElement server_connection_element;
        System.Text.Json.JsonElement custom_element;
        System.Text.Json.JsonElement custom_element2;
        System.Text.Json.JsonElement stamp_id_element;
        System.Text.Json.JsonElement server_ip_element;

        try
        {
            if (session_event.HttpClient.Response.HasBody)
            {
                response_body = await session_event.GetResponseBodyAsString();
                json_data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(response_body);
                if (json_data != null && json_data.ContainsKey("properties"))
                {
                    properties_element = json_data["properties"] as System.Text.Json.JsonElement?;
                    if (properties_element.HasValue)
                    {
                        if (properties_element.Value.TryGetProperty("system", out system_element) &&
                            system_element.TryGetProperty("matchmaking", out matchmaking_element) &&
                            matchmaking_element.TryGetProperty("serverConnectionString", out server_connection_element))
                        {
                            Utils.ft_set_server_name(server_connection_element.GetString());
                        }
                        if (properties_element.Value.TryGetProperty("custom", out custom_element) &&
                            custom_element.TryGetProperty("StampId", out stamp_id_element))
                        {
                            Utils.ft_set_stamp_id(stamp_id_element.GetString());
                        }
                        if (properties_element.Value.TryGetProperty("custom", out custom_element2) &&
                            custom_element2.TryGetProperty("GameServerAddress", out server_ip_element))
                        {
                            Utils.ft_set_server_ip(server_ip_element.GetString());
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error response: {e.Message}");
        }
    }

    private static async void ft_process_xuid(SessionEventArgs session_event)
    {
        string request_body;
        Dictionary<string, object> json_data;
        System.Text.Json.JsonElement? users_element;
        System.Text.Json.JsonElement xuid_element;
        string xuid;

        try
        {
            if (session_event.HttpClient.Request.HasBody)
            {
                request_body = await session_event.GetRequestBodyAsString();
                json_data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request_body);
                // Console.WriteLine($"data request: {json_data}");
                if (json_data != null && json_data.ContainsKey("Users"))
                {
                    users_element = json_data["Users"] as System.Text.Json.JsonElement?;
                    // Console.WriteLine($"Users element: {users_element?.ToString()}");
                    if (users_element.HasValue && users_element.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        // Console.WriteLine($"Processing {users_element.Value.GetArrayLength()} users.");
                        foreach (var user in users_element.Value.EnumerateArray())
                        {
                            // Console.WriteLine($"Processing user: {user}");
                            if (user.TryGetProperty("Xuid", out xuid_element))
                            {
                                // Console.WriteLine($"Found XUID element: {xuid_element}");
                                xuid = xuid_element.GetString();
                                if (!string.IsNullOrEmpty(xuid) && xuid.All(char.IsDigit))
                                {
                                    Utils.ft_add_xuid(xuid);
                                    // Utils.ft_print_xuids();
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error request: {e.Message}");
        }
    }

    private static async void ft_process_new_server(SessionEventArgs session_event)
    {
        string request_body;
        Dictionary<string, object> json_data;
        System.Text.Json.JsonElement? events_element;
        System.Text.Json.JsonElement name_element;

        try
        {
            if (session_event.HttpClient.Request.HasBody)
            {
                request_body = await session_event.GetRequestBodyAsString();
                json_data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request_body);
                if (json_data != null && json_data.ContainsKey("Events"))
                {
                    events_element = json_data["Events"] as System.Text.Json.JsonElement?;
                    if (events_element.HasValue && events_element.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var event_item in events_element.Value.EnumerateArray())
                        {
                            if (event_item.TryGetProperty("Name", out name_element))
                            {
                                if (name_element.GetString() == "client_connected_to_network")
                                {
                                    Utils.ft_clear_xuid_list();
                                    Utils.ft_set_server_name("unknow");
                                    Utils.ft_set_server_ip("unknow");
                                    Utils.ft_set_stamp_id("unknow");
                                    Utils.ft_clear_gt_info();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error request: {e.Message}");
        }
    }

    private class RareResponse
    {
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }
}