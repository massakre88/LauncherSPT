using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LauncherSPT
{
    public class SptConnectInfo
    {
        public List<string> Editions { get; set; } = new();
        public Dictionary<string, string> Descriptions { get; set; } = new();
    }

    // Fala diretamente com a API do servidor SPT (as mesmas rotas /launcher/... que o
    // SPT.Launcher.exe usa) para poder criar e apagar perfis sem precisar dele.
    public static class SptApiClient
    {
        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                // O servidor SPT usa um certificado autoassinado em localhost
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            // Pede ao servidor para não comprimir pedido/resposta (evita ter de lidar com zlib)
            client.DefaultRequestHeaders.Add("requestcompressed", "0");
            client.DefaultRequestHeaders.Add("responsecompressed", "0");

            return client;
        }

        private static string BaseUrl(string ip, int port) => $"https://{ip}:{port}";

        public static async Task<SptConnectInfo?> ConnectAsync(string ip, int port)
        {
            using var client = CreateClient();
            var content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl(ip, port)}/launcher/server/connect", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return null;

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var info = new SptConnectInfo();

            if (root.TryGetProperty("editions", out var editionsEl) && editionsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in editionsEl.EnumerateArray())
                {
                    var val = e.GetString();
                    if (!string.IsNullOrEmpty(val))
                        info.Editions.Add(val);
                }
            }

            if (root.TryGetProperty("profileDescriptions", out var descEl) && descEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in descEl.EnumerateObject())
                {
                    info.Descriptions[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            return info;
        }

        // Devolve o novo ID de perfil, ou null se falhou (ex.: username já existe)
        public static async Task<string?> RegisterProfileAsync(string ip, int port, string username, string edition)
        {
            using var client = CreateClient();

            var payload = JsonSerializer.Serialize(new { username, edition });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl(ip, port)}/launcher/profile/register", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
            return string.IsNullOrWhiteSpace(body) ? null : body;
        }

        // Apaga o perfil identificado por profileId (o mesmo valor usado como -token ao arrancar o jogo)
        public static async Task<bool> RemoveProfileAsync(string ip, int port, string profileId)
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"PHPSESSID={profileId}");

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{BaseUrl(ip, port)}/launcher/profile/remove", content);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = (await response.Content.ReadAsStringAsync()).Trim().ToLowerInvariant();
            return body == "true";
        }
    }
}
