using System.Net.Http;
using System.Text.Json.Nodes;
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

                return new PatientApiModel
                {
                    IdPasien = idPasienInput,
                    Nama = data?[ConfigService.GetMappingValue("NamaKey")]?.ToString() ?? "",
                    Gender = data?[ConfigService.GetMappingValue("GenderKey")]?.ToString() ?? "",
                    TanggalLahir = ParseDate(data?[ConfigService.GetMappingValue("TglLahirKey")]?.ToString())
                };
            }
            catch { return null; }
        }

        private DateTime? ParseDate(string rawDate)
        {
            if (DateTime.TryParse(rawDate, new System.Globalization.CultureInfo("id-ID"), System.Globalization.DateTimeStyles.None, out DateTime res)) return res;
            if (DateTime.TryParse(rawDate, out DateTime resUniversal)) return resUniversal;
            return null;
        }
    }
}