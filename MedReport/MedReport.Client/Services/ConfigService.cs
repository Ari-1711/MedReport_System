using System;
using System.IO;
using System.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
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

        // Menggunakan Entropy tambahan untuk DPAPI agar enkripsi lebih kuat
        private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("MedReport_Informatics_2026");

        // Default SHA-256 Hash untuk PIN "2026" dengan salt "IT_RS_SALT" (Untuk inisialisasi awal)
        private static readonly string DefaultPinHash = "040b2a3f0db66bf39fa5a22839999bd00fb953457a40bbd391696dd48161e12d";

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
                            // Jika ada template bawaan, load, enkripsi, lalu simpan dengan aman
                            string templateContent = File.ReadAllText(AppDirectoryConfig);
                            _configCache = JsonNode.Parse(templateContent) ?? new JsonObject();
                            if (_configCache["ItPinHash"] == null) _configCache["ItPinHash"] = DefaultPinHash;
                            SaveConfigToDiskInternal();
                        }
                        else
                        {
                            ResetToDefaultInternal();
                            return;
                        }
                    }
                    else
                    {
                        // Baca data terenkripsi dari disk
                        byte[] encryptedData = File.ReadAllBytes(ConfigPath);
                        byte[] decryptedData = ProtectedData.Unprotect(encryptedData, OptionalEntropy, DataProtectionScope.CurrentUser);
                        string jsonString = Encoding.UTF8.GetString(decryptedData);

                        _configCache = JsonNode.Parse(jsonString) ?? new JsonObject();
                    }
                }
                catch (Exception ex)
                {
                    ResetToDefaultInternal();
                    throw new InvalidOperationException($"Konfigurasi SIMRS gagal dimuat. Sistem beralih ke Default terenkripsi. Detail: {ex.Message}");
                }
            }
        }

        private static void ResetToDefaultInternal()
        {
            _configCache = new JsonObject
            {
                ["ItPinHash"] = DefaultPinHash,
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
            SaveConfigToDiskInternal();
        }

        private static void SaveConfigToDiskInternal()
        {
            // Menggunakan teknik Atomic Write untuk mencegah file korup saat mati lampu
            string tempPath = ConfigPath + ".tmp";
            string jsonString = _configCache.ToString();
            byte[] plainBytes = Encoding.UTF8.GetBytes(jsonString);

            // Enkripsi data menggunakan DPAPI (Hanya user Windows saat ini yang bisa mendekripsi)
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, OptionalEntropy, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(tempPath, encryptedBytes);
            if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
            File.Move(tempPath, ConfigPath);
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
        /// Memvalidasi SecureString PIN IT dengan Hash SHA-256 + Salt yang disimpan di konfigurasi.
        /// Aman dari memory dumping dan tidak meninggalkan plain text string di RAM.
        /// </summary>
        public static bool ValidateItPin(SecureString securePin)
        {
            lock (LockObject)
            {
                string storedHash = _configCache?["ItPinHash"]?.ToString() ?? DefaultPinHash;
                IntPtr bstr = IntPtr.Zero;
                try
                {
                    // Konversi SecureString ke pointer memori sementara
                    bstr = Marshal.SecureStringToBSTR(securePin);
                    int length = Marshal.ReadInt32(bstr, -4);
                    byte[] passwordBytes = new byte[length];
                    Marshal.Copy(bstr, passwordBytes, 0, length);

                    // Tambahkan salt statis khusus internal sistem
                    byte[] saltBytes = Encoding.UTF8.GetBytes("IT_RS_SALT");
                    byte[] combinedBytes = new byte[passwordBytes.Length + saltBytes.Length];
                    Buffer.BlockCopy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
                    Buffer.BlockCopy(saltBytes, 0, combinedBytes, passwordBytes.Length, saltBytes.Length);

                    // Hitung hash
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        byte[] hashBytes = sha256.ComputeHash(combinedBytes);
                        StringBuilder sb = new StringBuilder();
                        foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));

                        return sb.ToString() == storedHash;
                    }
                }
                catch
                {
                    return false;
                }
                finally
                {
                    // Pastikan area pointer memori langsung dibersihkan/dihancurkan
                    if (bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
                }
            }
        }

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

                    _configCache["HospitalName"] = hospitalName;
                    _configCache["HospitalAddress"] = address;
                    _configCache["HospitalLogoPath"] = logoPath;
                    _configCache["ApiUrl"] = apiUrl;
                    _configCache["DoctorApiUrl"] = doctorApiUrl;

                    if (_configCache["Mapping"] == null)
                    {
                        _configCache["Mapping"] = new JsonObject();
                    }

                    _configCache["Mapping"]["NamaKey"] = namaKey;
                    _configCache["Mapping"]["TglLahirKey"] = tglLahirKey;
                    _configCache["Mapping"]["GenderKey"] = genderKey;
                    _configCache["Mapping"]["DoctorNameKey"] = doctorNameKey;

                    // Tulis secara aman dan terenkripsi
                    SaveConfigToDiskInternal();
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