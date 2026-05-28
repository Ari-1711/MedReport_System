using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows; // Diperlukan untuk memunculkan MessageBox

namespace MedReport.Client.Services
{
    public static class ConfigService
    {
        // Jalur file konfigurasi di folder Configuration dalam direktori aplikasi
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "config.json");
        private static JsonNode _configCache;

        // NILAI FALLBACK (BAN SEREP): Nilai ini akan digunakan otomatis jika file JSON rusak/hilang
        private static readonly string DefaultApiUrl = "http://localhost:3000/pasien/";
        private static readonly string DefaultDoctorApiUrl = "http://localhost:3000/dokter";
        private static readonly string DefaultNamaKey = "nama_lengkap";
        private static readonly string DefaultTglLahirKey = "tgl_lahir";
        private static readonly string DefaultGenderKey = "jenis_kelamin";
        private static readonly string DefaultDoctorNameKey = "nama";

        public static void LoadConfig()
        {
            try
            {
                // 1. CEK KEBERADAAN FILE
                if (!File.Exists(ConfigPath))
                {
                    // Jika folder hilang, buat folder baru
                    string directoryName = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);

                    // Buat file JSON baru dengan isi kosong agar sistem tetap jalan
                    _configCache = new JsonObject();
                    File.WriteAllText(ConfigPath, _configCache.ToString());
                    return;
                }

                // 2. COBA BACA ISI JSON
                _configCache = JsonNode.Parse(File.ReadAllText(ConfigPath)) ?? new JsonObject();
            }
            catch (Exception ex)
            {
                // 3. NOTIFIKASI JIKA CORRUPT/RUSAK
                MessageBox.Show(
                    "File konfigurasi (config.json) rusak atau tidak terbaca. " +
                    "Sistem akan menggunakan pengaturan standar dan mencoba memulihkan file.\n\n" +
                    $"Detail Kesalahan: {ex.Message}",
                    "Pemulihan Konfigurasi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // 4. PEMULIHAN (SELF-HEALING)
                // Inisialisasi cache kosong agar properti GetValue tidak error
                _configCache = new JsonObject();

                // Tulis ulang file yang rusak dengan yang baru/bersih
                try
                {
                    string directoryName = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);
                    File.WriteAllText(ConfigPath, _configCache.ToString());
                }
                catch { /* Abaikan jika terjadi masalah hak akses disk */ }
            }
        }

        // Properti ApiUrl dengan logika pengecekan nilai kosong
        public static string ApiUrl
        {
            get
            {
                string url = GetValue("ApiUrl");
                // Jika di JSON kosong, kembalikan DefaultApiUrl (localhost)
                return string.IsNullOrWhiteSpace(url) ? DefaultApiUrl : url;
            }
        }

        public static string GetValue(string key) => _configCache?[key]?.ToString() ?? string.Empty;

        public static string GetMappingValue(string key)
        {
            // Ambil nilai dari bagian Mapping di JSON
            string value = _configCache?["Mapping"]?[key]?.ToString();

            // Jika nilai di JSON ada, gunakan itu
            if (!string.IsNullOrWhiteSpace(value)) return value;

            // JIKA KOSONG (KARENA FILE RUSAK): Gunakan kamus cadangan dari kode
            return key switch
            {
                "NamaKey" => DefaultNamaKey,
                "TglLahirKey" => DefaultTglLahirKey,
                "GenderKey" => DefaultGenderKey,
                "DoctorNameKey" => DefaultDoctorNameKey,
                _ => string.Empty
            };
        }

        public static string HospitalName => GetValue("HospitalName");
        public static string HospitalAddress => GetValue("HospitalAddress");
        public static string HospitalLogoPath => GetValue("HospitalLogoPath");

        public static bool SaveTemplate(string hospitalName, string address, string logoPath)
        {
            try
            {
                // Pastikan cache sudah terinisialisasi
                if (_configCache == null) LoadConfig();

                // Pastikan folder 'Configuration' ada sebelum menulis file
                string directoryName = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                _configCache["HospitalName"] = hospitalName;
                _configCache["HospitalAddress"] = address;
                _configCache["HospitalLogoPath"] = logoPath;

                // Simpan perubahan ke file fisik
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