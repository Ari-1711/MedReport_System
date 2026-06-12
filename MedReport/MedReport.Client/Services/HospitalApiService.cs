using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MedReport.Client.Models;

namespace MedReport.Client.Services
{
    public class HospitalApiService
    {
        private readonly HttpClient _client;

        public HospitalApiService()
        {
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        public async Task<PatientApiModel?> CariPasienAsync(string idPasienInput)
        {
            try
            {
                string apiUrl = ConfigService.ApiUrl;
                if (string.IsNullOrEmpty(apiUrl)) return null;

                HttpResponseMessage response = await _client.GetAsync($"{apiUrl}{idPasienInput}");
                if (!response.IsSuccessStatusCode) return null;

                var data = JsonNode.Parse(await response.Content.ReadAsStringAsync());

                // AMBIL DATA DOKTER/TENANT DARI CONFIG JIKA ADA (Mendukung Multi-tenancy)
                string currentHospitalId = ConfigService.HospitalLogoPath ?? "DEFAULT_RS";

                return new PatientApiModel
                {
                    HospitalId = currentHospitalId, // Isi pengunci Tenant ID
                    IdPasien = idPasienInput,
                    Nama = data?[ConfigService.GetMappingValue("NamaKey")]?.ToString() ?? "",

                    // SOLUSI 1: Petakan ke RawGender agar logika normalisasi Enum di Model bekerja
                    RawGender = data?[ConfigService.GetMappingValue("GenderKey")]?.ToString() ?? "",

                    // SOLUSI 2: Gunakan null-coalescing (??) untuk memaksa DateTime? menjadi DateTime murni
                    TanggalLahir = ParseDate(data?[ConfigService.GetMappingValue("TglLahirKey")]?.ToString()) ?? DateTime.MinValue
                };
            }
            catch { return null; }
        }

        private DateTime? ParseDate(string rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return null;

            if (DateTime.TryParse(rawDate, new System.Globalization.CultureInfo("id-ID"), System.Globalization.DateTimeStyles.None, out DateTime res)) return res;
            if (DateTime.TryParse(rawDate, out DateTime resUniversal)) return resUniversal;
            return null;
        }
    }
}