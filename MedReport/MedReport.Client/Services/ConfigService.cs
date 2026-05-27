using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MedReport.Client.Services
{
    public static class ConfigService
    {
        // Jalur file sesuai gambar properti Anda (Folder Configuration)
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "config.json");
        private static JsonNode _configCache;

        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                    _configCache = new JsonObject();
                    File.WriteAllText(ConfigPath, _configCache.ToString());
                    return;
                }
                _configCache = JsonNode.Parse(File.ReadAllText(ConfigPath)) ?? new JsonObject();
            }
            catch { _configCache = new JsonObject(); }
        }

        public static string GetValue(string key) => _configCache?[key]?.ToString() ?? string.Empty;
        public static string GetMappingValue(string key) => _configCache?["Mapping"]?[key]?.ToString() ?? string.Empty;

        public static string HospitalName => GetValue("HospitalName");
        public static string HospitalAddress => GetValue("HospitalAddress");

        public static string HospitalLogoPath => GetValue("HospitalLogoPath");

        public static bool SaveTemplate(string hospitalName, string address, string logoPath)
        {
            try
            {
                if (_configCache == null) LoadConfig();
                _configCache["HospitalName"] = hospitalName;
                _configCache["HospitalAddress"] = address;
                _configCache["HospitalLogoPath"] = logoPath;
                File.WriteAllText(ConfigPath, _configCache.ToString());
                return true;
            }
            catch { return false; }
        }

        public static string ApiUrl => GetValue("ApiUrl");


    }
}