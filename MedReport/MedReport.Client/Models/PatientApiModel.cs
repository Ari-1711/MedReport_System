using System;
using System.Text.Json.Serialization;

namespace MedReport.Client.Models
{
    public enum GenderType
    {
        Unknown = 0,
        LakiLaki = 1,
        Perempuan = 2
    }

    public class PatientApiModel
    {
        public string HospitalId { get; set; } = string.Empty;
        public string IdPasien { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;

        /// <summary>
        /// Menggunakan string mentah dari API untuk menghindari crash parser akibat perbedaan format kultur regional (budaya komputer) di RS.
        /// </summary>
        [JsonPropertyName("TanggalLahirRaw")]
        public string RawTanggalLahir { get; set; } = string.Empty;

        /// <summary>
        /// Properti jembatan untuk kalkulasi medis klinis yang aman.
        /// </summary>
        [JsonIgnore]
        public DateTime TanggalLahir
        {
            get
            {
                if (DateTime.TryParse(RawTanggalLahir, out DateTime parsedDate))
                {
                    return parsedDate;
                }
                return DateTime.MinValue; // Mengembalikan ban serep jika format RS aneh
            }
        }

        [JsonPropertyName("Gender")]
        public string RawGender { get; set; } = string.Empty;

        [JsonIgnore]
        public GenderType NormalizedGender
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RawGender)) return GenderType.Unknown;

                string cleanInput = RawGender.Trim().ToUpper();

                // Ditambahkan toleransi angka standar kodefikasi rekam medis (1 = L, 2 = P)
                if (cleanInput == "L" || cleanInput == "LAKI-LAKI" || cleanInput == "LAKILAKI" || cleanInput == "MALE" || cleanInput == "1")
                    return GenderType.LakiLaki;

                if (cleanInput == "P" || cleanInput == "PEREMPUAN" || cleanInput == "FEMALE" || cleanInput == "2")
                    return GenderType.Perempuan;

                return GenderType.Unknown;
            }
        }
    }
}