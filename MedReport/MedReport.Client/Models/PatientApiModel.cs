using System;
using System.Text.Json.Serialization;

namespace MedReport.Client.Models
{
    /// <summary>
    /// Representasi standar Jenis Kelamin klinis untuk menjaga konsistensi format data antar-RS.
    /// </summary>
    public enum GenderType
    {
        Unknown = 0,
        LakiLaki = 1,
        Perempuan = 2
    }

    /// <summary>
    /// Merepresentasikan model data pasien yang digunakan untuk berinteraksi dengan API Rumah Sakit (Telah Dioptimalkan).
    /// </summary>
    public class PatientApiModel
    {
        /// <summary>
        /// KUNCI MULTI-TENANT: Mengisolasi data pasien agar tidak bocor atau tertukar antar rumah sakit.
        /// </summary>
        public string HospitalId { get; set; } = string.Empty;

        /// <summary>
        /// Nomor rekam medis atau identitas unik pasien.
        /// </summary>
        public string IdPasien { get; set; } = string.Empty;

        /// <summary>
        /// Nama lengkap pasien.
        /// </summary>
        public string Nama { get; set; } = string.Empty;

        /// <summary>
        /// DATA VITAL: Dibuat non-nullable karena wajib digunakan untuk kalkulasi klinis (misal: dosis obat).
        /// </summary>
        public DateTime TanggalLahir { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Format String mentah dari API luar (Mendukung fleksibilitas "L", "P", "Laki-laki").
        /// Properti ini disembunyikan dari binding UI langsung.
        /// </summary>
        [JsonPropertyName("Gender")]
        public string RawGender { get; set; } = string.Empty;

        /// <summary>
        /// NORMALISASI DATA: Properti internal aplikasi yang otomatis menerjemahkan data mentah API menjadi Enum yang konsisten.
        /// </summary>
        [JsonIgnore]
        public GenderType NormalizedGender
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RawGender)) return GenderType.Unknown;

                string cleanInput = RawGender.Trim().ToUpper();
                if (cleanInput == "L" || cleanInput == "LAKI-LAKI" || cleanInput == "LAKILAKI" || cleanInput == "MALE")
                    return GenderType.LakiLaki;

                if (cleanInput == "P" || cleanInput == "PEREMPUAN" || cleanInput == "FEMALE")
                    return GenderType.Perempuan;

                return GenderType.Unknown;
            }
        }
    }
}