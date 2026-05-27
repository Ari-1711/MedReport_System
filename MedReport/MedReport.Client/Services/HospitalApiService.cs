using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MedReport.Client.Models;
using System.Linq;

namespace MedReport.Client.Services
{
    public class HospitalApiService
    {
        // -------------------------------------------------------------------------
        // SINGLETON HTTP CLIENT: Mencegah Socket Exhaustion (Kebocoran Port Jaringan)
        // -------------------------------------------------------------------------
        private readonly HttpClient _client;

        public HospitalApiService()
        {
            // Inisialisasi dilakukan hanya sekali saat aplikasi menyala
            _client = new HttpClient();

            // SABUK PENGAMAN JARINGAN (TIMEOUT)
            // Jaringan LAN rumah sakit sering tidak stabil. Jika server API "hidup segan mati tak mau",
            // aplikasi akan otomatis menyerah dan memutus koneksi dalam 5 detik daripada membeku (freeze) selamanya.
            _client.Timeout = TimeSpan.FromSeconds(5);
        }

        // -------------------------------------------------------------------------
        // FUNGSI PENCARIAN ASINKRON: Mengambil data pasien tanpa mengunci layar UI
        // -------------------------------------------------------------------------
        public async Task<PatientApiModel?> CariPasienAsync(string idPasienInput)
        {
            try
            {
                // 1. MEMBACA PETA API (DYNAMIC CONFIGURATION)
                // Mengambil URL dan kunci JSON dari file eksternal (config.json) agar teknisi lapangan 
                // bisa menyesuaikan aplikasi dengan server rumah sakit yang berbeda-beda 
                // tanpa perlu kompilasi ulang kode sumber (.exe).
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "config.json");

                if (!File.Exists(configPath)) return null;

                string configText = await File.ReadAllTextAsync(configPath);
                var config = JsonNode.Parse(configText);

                string apiUrl = config["ApiUrl"]?.ToString() ?? "";
                var mapping = config["Mapping"];
                string namaKey = mapping["NamaKey"]?.ToString() ?? "";
                string tglLahirKey = mapping["TglLahirKey"]?.ToString() ?? "";
                string genderKey = mapping["GenderKey"]?.ToString() ?? "";

                // 2. MENGIRIM REQUEST KE SERVER 
                string requestUrl = $"{apiUrl}{idPasienInput}";
                HttpResponseMessage response = await _client.GetAsync(requestUrl);

                // Jika server membalas error (misal 404 Not Found / 500 Server Error), 
                // langsung pulang membawa nilai kosong (null) untuk ditangani oleh UI.
                if (!response.IsSuccessStatusCode) return null;

                // 3. BONGKAR PAKET JSON DARI SERVER
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JsonNode.Parse(jsonResponse);

                // -------------------------------------------------------------------------
                // 4. PARSING TANGGAL (TYPE-SAFE) - TITIK RAWAN FATALITAS
                // -------------------------------------------------------------------------
                string rawDate = data?[tglLahirKey]?.ToString() ?? "";
                DateTime? parsedDate = null;

                // STRATEGI FALLBACK (Rencana Cadangan Multi-Format)
                // Skenario A: Coba parsing sebagai format lokal Indonesia (misal: "14 Mei 2026" atau "14/05/2026")
                if (DateTime.TryParse(rawDate, new System.Globalization.CultureInfo("id-ID"), System.Globalization.DateTimeStyles.None, out DateTime tempDate))
                {
                    parsedDate = tempDate;
                }
                // Skenario B: Jika tim IT rumah sakit diam-diam mengubah API mereka ke format Universal/ISO 8601 (misal: "2026-05-14T00:00:00Z")
                else if (DateTime.TryParse(rawDate, out DateTime universalDate))
                {
                    parsedDate = universalDate;
                }
                // Jika kedua skenario gagal, parsedDate tetap null. 
                // Tanggal Lahir di UI hanya akan menjadi kosong, mencegah aplikasi mengalami force close.

                // 5. KEMAS KE DALAM MODEL STANDAR APLIKASI
                return new PatientApiModel
                {
                    IdPasien = idPasienInput,
                    Nama = data?[namaKey]?.ToString() ?? "",
                    TanggalLahir = parsedDate, // Oper data sebagai objek DateTime murni ke kalender WPF
                    Gender = data?[genderKey]?.ToString() ?? ""
                };
            }
            catch (Exception)
            {
                // SILENT FAILURE (Kegagalan Senyap)
                // Jika terjadi error ekstrem (file config hilang, format JSON rusak parah),
                // tangkap errornya di sini dan kembalikan null. 
                // Biarkan View (UI) yang bertanggung jawab memunculkan MessageBox peringatan ke pengguna.
                return null;
            }
        }
    }
}