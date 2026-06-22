using System;
using System.IO;
using System.Text.Json.Nodes;

namespace MedReport.Client.Services
{
    public static class ConfigService
    {
        private static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalApp_Api", "Configuration");
        private static readonly string ConfigPath = Path.Combine(ConfigFolder, "config.json");
        private static readonly string AppDirectoryConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "config.json");

        private static JsonNode _configCache = new JsonObject();
        private static readonly object LockObject = new object();

        // FALLBACK VALUES
        private static readonly string DefaultApiUrl = "http://localhost:3000/pasien/";
        private static readonly string DefaultDoctorApiUrl = "http://localhost:3000/dokter";
        private static readonly string DefaultNamaKey = "nama_lengkap";
        private static readonly string DefaultTglLahirKey = "tgl_lahir";
        private static readonly string DefaultGenderKey = "jenis_kelamin";
        private static readonly string DefaultDoctorNameKey = "nama";

        public static void LoadConfig()
        {
            lock (LockObject)
            {
                try
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    if (!File.Exists(ConfigPath))
                    {
                        if (File.Exists(AppDirectoryConfig))
                        {
                            File.Copy(AppDirectoryConfig, ConfigPath, true);
                        }
                        else
                        {
                            ResetToDefaultInternal();
                            return;
                        }
                    }

                    _configCache = JsonNode.Parse(File.ReadAllText(ConfigPath)) ?? new JsonObject();
                }
                catch (Exception ex)
                {
                    ResetToDefaultInternal();
                    throw new InvalidOperationException($"Konfigurasi SIMRS gagal dimuat. Sistem beralih ke Default. Detail: {ex.Message}");
                }
            }
        }

        private static void ResetToDefaultInternal()
        {
            _configCache = new JsonObject
            {
                ["HospitalName"] = "",
                ["HospitalAddress"] = "",
                ["HospitalLogoPath"] = "",
                ["ApiUrl"] = DefaultApiUrl,
                ["DoctorApiUrl"] = DefaultDoctorApiUrl,
                ["Mapping"] = new JsonObject
                {
                    ["NamaKey"] = DefaultNamaKey,
                    ["TglLahirKey"] = DefaultTglLahirKey,
                    ["GenderKey"] = DefaultGenderKey,
                    ["DoctorNameKey"] = DefaultDoctorNameKey
                }
            };
            File.WriteAllText(ConfigPath, _configCache.ToString());
        }

        public static string ApiUrl => GetValue("ApiUrl") == string.Empty ? DefaultApiUrl : GetValue("ApiUrl");
        public static string DoctorApiUrl => GetValue("DoctorApiUrl") == string.Empty ? DefaultDoctorApiUrl : GetValue("DoctorApiUrl");
        public static string HospitalName => GetValue("HospitalName");
        public static string HospitalAddress => GetValue("HospitalAddress");
        public static string HospitalLogoPath => GetValue("HospitalLogoPath");

        public static string GetValue(string key)
        {
            lock (LockObject) return _configCache?[key]?.ToString() ?? string.Empty;
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

        /// <summary>
        /// REVISI QA: Fungsi simpan yang komplit untuk meredam bug kehilangan data saat Admin mengklik tombol "Simpan" di UI.
        /// </summary>
        public static bool SaveFullConfiguration(
            string hospitalName, string address, string logoPath,
            string apiUrl, string doctorApiUrl,
            string namaKey, string tglLahirKey, string genderKey, string doctorNameKey)
        {
            lock (LockObject)
            {
                try
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    // 1. Simpan Data Identitas RS
                    _configCache["HospitalName"] = hospitalName;
                    _configCache["HospitalAddress"] = address;
                    _configCache["HospitalLogoPath"] = logoPath;

                    // 2. Simpan Endpoint API
                    _configCache["ApiUrl"] = apiUrl;
                    _configCache["DoctorApiUrl"] = doctorApiUrl;

                    // 3. Simpan Kunci Mapping Dinamis (Mencegah Null Object)
                    if (_configCache["Mapping"] == null)
                    {
                        _configCache["Mapping"] = new JsonObject();
                    }

                    _configCache["Mapping"]["NamaKey"] = namaKey;
                    _configCache["Mapping"]["TglLahirKey"] = tglLahirKey;
                    _configCache["Mapping"]["GenderKey"] = genderKey;
                    _configCache["Mapping"]["DoctorNameKey"] = doctorNameKey;

                    // Tulis ke file fisik lokal RS
                    File.WriteAllText(ConfigPath, _configCache.ToString());
                    return true;
                }
                catch
                {
                    return false; // Mengembalikan false jika gagal akses disk (I/O Error)
                }
            }
        }
    }
}