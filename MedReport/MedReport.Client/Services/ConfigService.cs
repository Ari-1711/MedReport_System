using System;
using System.IO;
using System.Text.Json.Nodes;

namespace MedReport.Client.Services
{
    public static class ConfigService
    {
        // SOLUSI 1: Pindahkan jalur file ke AppData/Local demi menghindari UnauthorizedAccessException di Program Files
        private static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalApp_Api", "Configuration");
        private static readonly string ConfigPath = Path.Combine(ConfigFolder, "config.json");

        private static JsonNode _configCache = new JsonObject();

        // SOLUSI 3: Gembok sinkronisasi untuk menjamin Thread-Safety pada akses data statis
        private static readonly object LockObject = new object();

        // NILAI FALLBACK (BAN SEREP)
        private static readonly string DefaultApiUrl = "http://localhost:3000/pasien/";
        private static readonly string DefaultDoctorApiUrl = "http://localhost:3000/dokter";
        private static readonly string DefaultNamaKey = "nama_lengkap";
        private static readonly string DefaultTglLahirKey = "tgl_lahir";
        private static readonly string DefaultGenderKey = "jenis_kelamin";
        private static readonly string DefaultDoctorNameKey = "nama";

        public static void LoadConfig()
        {
            lock (LockObject) // Kunci thread saat membaca file fisik
            {
                try
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    if (!File.Exists(ConfigPath))
                    {
                        _configCache = new JsonObject();
                        File.WriteAllText(ConfigPath, _configCache.ToString());
                        return;
                    }

                    _configCache = JsonNode.Parse(File.ReadAllText(ConfigPath)) ?? new JsonObject();
                }
                catch (Exception ex)
                {
                    // SOLUSI 2: Jangan panggil MessageBox di layer service! Lempar Exception terkontrol 
                    // agar ditangkap oleh App.xaml.cs Global Exception Handler untuk ditampilkan ke UI
                    _configCache = new JsonObject();

                    try
                    {
                        File.WriteAllText(ConfigPath, _configCache.ToString());
                    }
                    catch { /* Abaikan jika disk gagal total */ }

                    throw new InvalidOperationException($"Konfigurasi sistem (config.json) korup. Berhasil dipulihkan ke pengaturan awal. Detail: {ex.Message}");
                }
            }
        }

        public static string ApiUrl
        {
            get
            {
                lock (LockObject) // Kunci thread saat membaca properti
                {
                    string url = GetValueInternal("ApiUrl");
                    return string.IsNullOrWhiteSpace(url) ? DefaultApiUrl : url;
                }
            }
        }

        // Fungsi internal yang tidak memakai lock sendiri untuk menghindari risiko Deadlock
        private static string GetValueInternal(string key) => _configCache?[key]?.ToString() ?? string.Empty;

        public static string GetValue(string key)
        {
            lock (LockObject)
            {
                return GetValueInternal(key);
            }
        }

        public static string GetMappingValue(string key)
        {
            lock (LockObject)
            {
                string value = _configCache?["Mapping"]?[key]?.ToString();

                if (!string.IsNullOrWhiteSpace(value)) return value;

                return key switch
                {
                    "NamaKey" => DefaultNamaKey,
                    "TglLahirKey" => DefaultTglLahirKey,
                    "GenderKey" => DefaultGenderKey,
                    "DoctorNameKey" => DefaultDoctorNameKey,
                    _ => string.Empty
                };
            }
        }

        public static string HospitalName => GetValue("HospitalName");
        public static string HospitalAddress => GetValue("HospitalAddress");
        public static string HospitalLogoPath => GetValue("HospitalLogoPath");

        public static bool SaveTemplate(string hospitalName, string address, string logoPath)
        {
            lock (LockObject) // Kunci thread saat memodifikasi cache dan menulis ke disk
            {
                try
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    _configCache["HospitalName"] = hospitalName;
                    _configCache["HospitalAddress"] = address;
                    _configCache["HospitalLogoPath"] = logoPath;

                    File.WriteAllText(ConfigPath, _configCache.ToString());
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}