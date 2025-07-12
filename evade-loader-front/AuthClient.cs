using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace ProxyLoader
{
    public class AuthClient
    {
        private const string AUTH_FILE_EVADE = "C:\\SotSolution\\authBanEvade.json";
        private const string AUTH_FILE_PLAYERLIST = "C:\\SotSolution\\authPlayerlist.json";
        private const string EVADE_API_URL = "http://XXX.XX.XXX.XXX:XXXX";
        private const string PLAYERLIST_API_URL = "http://XXX.XX.XXX.XXX:XXXX";
        private readonly HttpClient _client;
        private string _username;
        private int _daysRemaining;
        private bool _requires2FA;
        private string _lastUsedPassword;
        private string _currentApiUrl;
        private string _currentAuthFile;

        public AuthClient(string product = "Evade")
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "SotSolution-Loader/1.0");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _username = null;
            _daysRemaining = 0;
            _requires2FA = false;
            _lastUsedPassword = null;
            _currentApiUrl = product == "Evade" ? EVADE_API_URL : PLAYERLIST_API_URL;
            _currentAuthFile = product == "Evade" ? AUTH_FILE_EVADE : AUTH_FILE_PLAYERLIST;
            
            // Log l'URL utilisée pour faciliter le débogage
            LogDebug($"Initializing AuthClient with API URL: {_currentApiUrl}");
        }

        public async Task<(bool success, bool requires2FA)> Login(string username, string password)
        {
            try
            {
                _lastUsedPassword = password;
                var content = new StringContent(
                    JsonSerializer.Serialize(new { username, password }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");
                
                LogDebug($"Attempting login for user: {username}");
                
                var response = await _client.PostAsync($"{_currentApiUrl}/login", content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                LogDebug($"Login response: Status={response.StatusCode}, Body={responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(result);
                    _username = username;
                    _daysRemaining = loginResponse.days_remaining;
                    _requires2FA = false;
                    return (true, false);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(result);
                    
                    // Vérifier si une 2FA est nécessaire
                    if (errorResponse?.requiresVerification == true)
                    {
                        _username = username;
                        _requires2FA = true;
                        return (false, true);
                    }
                    else
                    {
                        // Si IP incorrecte mais pas de 2FA requise (message d'erreur standard de l'API)
                        LogDebug($"Access denied but no 2FA required: {errorResponse?.message}");
                        return (false, false);
                    }
                }
                return (false, false);
            }
            catch (Exception ex)
            {
                // Log de l'erreur pour débogage
                LogDebug($"Login Error: {ex.Message}");
                try {
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "auth_error.log"),
                        $"Login Error: {ex.Message}\n{ex.StackTrace}"
                    );
                } catch { }
                return (false, false);
            }
        }

        public async Task<bool> Verify2FA(string code)
        {
            if (string.IsNullOrEmpty(_username))
            {
                LogDebug("Verify2FA called with empty username");
                return false;
            }
            
            // Si nous n'avons pas besoin de 2FA (IP déjà connue), retourner directement true
            if (!_requires2FA)
            {
                LogDebug("Verification not required (IP already authorized)");
                return true;
            }

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { username = _username, code }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");
                
                LogDebug($"Sending verification request: {_username}, code: {code}");
                
                var response = await _client.PostAsync($"{_currentApiUrl}/verify-2fa", content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                LogDebug($"Verification response status: {response.StatusCode}, body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    LogDebug("Verification successful, updating login status");
                    
                    // Une fois la 2FA validée, on marque comme validé sans refaire un login
                    _requires2FA = false;
                    return true;
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
                        LogDebug($"Server error message: {errorResponse?.message}");
                    }
                    catch
                    {
                        LogDebug("Could not parse error response");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Verification exception: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegisterDiscord(string discordUsername, string apiKey)
        {
            if (string.IsNullOrEmpty(_username))
                return false;

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { 
                        username = _username, 
                        discord_username = discordUsername, 
                        api_key = apiKey 
                    }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");
                
                var response = await _client.PostAsync($"{_currentApiUrl}/register-discord", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log de l'erreur pour débogage
                try {
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "discord_error.log"),
                        $"Discord Registration Error: {ex.Message}\n{ex.StackTrace}"
                    );
                } catch { }
                return false;
            }
        }

        public void SaveCredentials(string username, string password)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_currentAuthFile));
            var credentials = new { username, password };
            File.WriteAllText(_currentAuthFile, JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true }));
        }

        public (string username, string password) LoadCredentials()
        {
            if (File.Exists(_currentAuthFile))
            {
                var json = File.ReadAllText(_currentAuthFile);
                var credentials = JsonSerializer.Deserialize<Credentials>(json);
                return (credentials.username, credentials.password);
            }
            return (null, null);
        }

        public string GetUsername() => _username;
        public int GetDaysRemaining() => _daysRemaining;
        public bool Requires2FA() => _requires2FA;

        // Méthode utilitaire pour logger les messages de debug
        private void LogDebug(string message)
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "auth_debug.log");
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    sw.WriteLine($"[{DateTime.Now}] {message}");
                }
            }
            catch { /* Ignore logging errors */ }
        }
    }

    public class LoginResponse
    {
        public int days_remaining { get; set; }
    }

    public class ErrorResponse
    {
        public string message { get; set; }
        public bool requiresVerification { get; set; }
    }

    public class Credentials
    {
        public string username { get; set; }
        public string password { get; set; }
    }
} 