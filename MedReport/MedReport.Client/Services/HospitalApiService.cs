using System;
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
            try
            {
                string apiUrl = ConfigService.ApiUrl;
                if (string.IsNullOrEmpty(apiUrl)) return null;

                HttpResponseMessage response = await _client.GetAsync($"{apiUrl}{idPasienInput}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Server Rumah Sakit merespons dengan kode status: {(int)response.StatusCode} ({response.ReasonPhrase})");
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonNode.Parse(responseBody);

                if (data == null) return null;

                string currentHospitalId = ConfigService.HospitalLogoPath ?? "DEFAULT_RS";

                string namaKey = ConfigService.GetMappingValue("NamaKey");
                string genderKey = ConfigService.GetMappingValue("GenderKey");
                string tglLahirKey = ConfigService.GetMappingValue("TglLahirKey");

                return new PatientApiModel
                {
                    HospitalId = currentHospitalId,
                    IdPasien = idPasienInput,
                    Nama = data[namaKey]?.ToString()?.Trim() ?? string.Empty,
                    RawGender = data[genderKey]?.ToString()?.Trim() ?? string.Empty,
                    TanggalLahir = ParseDate(data[tglLahirKey]?.ToString()) ?? DateTime.MinValue
                };
            }
            // PERBAIKAN: Kata kunci 'private' telah dihapus agar sintaksis catch valid
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

        private DateTime? ParseDate(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return null;

            if (DateTime.TryParse(rawDate, new System.Globalization.CultureInfo("id-ID"), System.Globalization.DateTimeStyles.None, out DateTime res)) return res;
            if (DateTime.TryParse(rawDate, out DateTime resUniversal)) return resUniversal;
            return null;
        }
    }
}