using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MedReport.Client.Models;

namespace MedReport.Client.Services
{
    public class HospitalApiService
    {
        // Berbagi satu instans HttpClient statis tunggal untuk mencegah Socket Exhaustion
        private static readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public async Task<PatientApiModel?> CariPasienAsync(string idPasienInput)
        {
            if (string.IsNullOrWhiteSpace(idPasienInput)) return null;

            try
            {
                string apiUrl = ConfigService.ApiUrl;
                if (string.IsNullOrEmpty(apiUrl)) return null;

                // KEAMANAN: Sanitasi input parameter URL untuk mencegah SSRF / Path Traversal
                string sanitizedId = Uri.EscapeDataString(idPasienInput.Trim());

                // Memastikan format URL digabung dengan benar
                string finalUrl = apiUrl.EndsWith("/") ? $"{apiUrl}{sanitizedId}" : $"{apiUrl}/{sanitizedId}";

                // EFISIENSI: Gunakan HttpCompletionOption.ResponseHeadersRead untuk membaca stream langsung
                using (HttpResponseMessage response = await _client.GetAsync(finalUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Server Rumah Sakit merespons dengan kode status: {(int)response.StatusCode} ({response.ReasonPhrase})");
                    }

                    // EFISIENSI: Membaca langsung dari stream jaringan tanpa alokasi string besar di RAM (Zero-Copy Mindset)
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        var data = JsonNode.Parse(stream);
                        if (data == null) return null;

                        string currentHospitalId = ConfigService.HospitalLogoPath ?? "DEFAULT_RS";
                        string namaKey = ConfigService.GetMappingValue("NamaKey");
                        string genderKey = ConfigService.GetMappingValue("GenderKey");
                        string tglLahirKey = ConfigService.GetMappingValue("TglLahirKey");

                        string rawGenderValue = data[genderKey]?.ToString()?.Trim() ?? string.Empty;

                        // Bagian dalam dari HospitalApiService.cs saat membuat objek baru:
                        return new PatientApiModel
                        {
                            HospitalId = currentHospitalId,
                            IdPasien = idPasienInput,
                            Nama = data[namaKey]?.ToString()?.Trim() ?? string.Empty,

                            // Tembak langsung ke properti RAW, biarkan model yang melakukan TryParse otomatis
                            RawGender = data[genderKey]?.ToString()?.Trim() ?? string.Empty,
                            RawTanggalLahir = data[tglLahirKey]?.ToString()?.Trim() ?? string.Empty
                        };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Koneksi ke server Rumah Sakit terputus (RTO / Timeout). Harap periksa jaringan lokal Anda.");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Gagal menghubungi server rekam medis RS. Detail: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new FormatException($"Format data yang dikirim oleh server RS tidak valid atau telah berubah. Detail: {ex.Message}");
            }
        }

        /// <summary>
        /// Menormalisasi format gender dari SIMRS lokal (Numerik/Alfabetis) ke Standar WHO
        /// </summary>
        private string NormalizeGender(string rawGender)
        {
            if (string.IsNullOrWhiteSpace(rawGender)) return "Unknown";

            string normalized = rawGender.ToUpperInvariant();

            // Standar WHO & Ragam Budaya Data SIMRS RS Lokal
            if (normalized == "1" || normalized == "M" || normalized == "MALE" || normalized == "L" || normalized == "LAKI-LAKI")
            {
                return "Male";
            }
            if (normalized == "2" || normalized == "F" || normalized == "FEMALE" || normalized == "P" || normalized == "PEREMPUAN")
            {
                return "Female";
            }

            return "Unknown";
        }

        private DateTime? ParseDate(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return null;

            if (DateTime.TryParse(rawDate, new System.Globalization.CultureInfo("id-ID"), System.Globalization.DateTimeStyles.None, out DateTime res)) return res;
            if (DateTime.TryParse(rawDate, out DateTime resUniversal)) return resUniversal;
            return null;
        }
    }
}