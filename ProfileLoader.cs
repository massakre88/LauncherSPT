using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LauncherSPT
{
    public static class ProfileLoader
    {
        // Lê todos os perfis em <pastaSPT>\user\profiles\*.json
        public static List<SptProfile> LoadProfiles(string sptFolder)
        {
            var result = new List<SptProfile>();

            var profilesFolder = Path.Combine(sptFolder, "user", "profiles");
            if (!Directory.Exists(profilesFolder))
                return result;

            foreach (var file in Directory.GetFiles(profilesFolder, "*.json"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    var root = doc.RootElement;

                    string username = "";
                    string edition = "";
                    if (root.TryGetProperty("info", out var info))
                    {
                        username = info.TryGetProperty("username", out var u) ? (u.GetString() ?? "") : "";
                        edition = info.TryGetProperty("edition", out var ed) ? (ed.GetString() ?? "") : "";
                    }

                    string nickname = "";
                    string side = "";
                    int level = 0;
                    if (root.TryGetProperty("characters", out var characters) &&
                        characters.TryGetProperty("pmc", out var pmc) &&
                        pmc.TryGetProperty("Info", out var pmcInfo))
                    {
                        nickname = pmcInfo.TryGetProperty("Nickname", out var nick) ? (nick.GetString() ?? "") : "";
                        side = pmcInfo.TryGetProperty("Side", out var s) ? (s.GetString() ?? "") : "";
                        level = pmcInfo.TryGetProperty("Level", out var lvl) && lvl.ValueKind == JsonValueKind.Number ? lvl.GetInt32() : 0;
                    }

                    var profile = new SptProfile
                    {
                        Id = Path.GetFileNameWithoutExtension(file),
                        Username = username,
                        Nickname = string.IsNullOrWhiteSpace(nickname) ? username : nickname,
                        Side = side,
                        Level = level,
                        GameVersion = FormatVersion(edition)
                    };

                    result.Add(profile);
                }
                catch
                {
                    // ignora ficheiros de perfil com estrutura inesperada/corrompida
                }
            }

            result.Sort((a, b) => string.Compare(a.Nickname, b.Nickname, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static string FormatVersion(string? raw)
        {
            return raw?.ToLowerInvariant() switch
            {
                "standard" => "Standard",
                "left_behind" => "Left Behind",
                "prepare_for_escape" => "Prepare for Escape",
                "edge_of_darkness" => "EOD",
                "unheard_edition" => "Unheard",
                _ => raw ?? ""
            };
        }

        // Lê o IP/porta reais configurados no servidor (SPT_Data\database\server.json), se existir
        public static (string Ip, int Port)? ReadServerAddress(string sptFolder)
        {
            var file = Path.Combine(sptFolder, "SPT_Data", "database", "server.json");
            if (!File.Exists(file))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;

                var ip = root.TryGetProperty("ip", out var ipEl) ? (ipEl.GetString() ?? "127.0.0.1") : "127.0.0.1";
                var port = root.TryGetProperty("port", out var portEl) ? portEl.GetInt32() : 6969;

                return (ip, port);
            }
            catch
            {
                return null;
            }
        }
    }
}
