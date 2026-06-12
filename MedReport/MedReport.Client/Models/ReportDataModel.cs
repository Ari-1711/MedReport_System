using System;
using System.Collections.Generic;
using System.Globalization;

namespace MedReport.Client.Models
{
    /// <summary>
    /// Manifes data lengkap yang digunakan sebagai sumber utama penyusunan laporan PDF oleh ReportService.
    /// </summary>
    public class ReportDataModel
    {
        // =========================================================================
        // DATA DEMOGRAFI PASIEN (MURNI & TERKUNCI)
        // =========================================================================
        public string IdPasien { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// SINKRONISASI TIPE DATA: Menggunakan DateTime murni untuk mencegah runtime bug lintas regional OS.
        /// </summary>
        public DateTime TanggalLahir { get; set; } = DateTime.MinValue;

        /// <summary>
        /// KULTUR TERKUNCI: Properti pembantu untuk menghasilkan teks tanggal seragam (Format Indonesia),
        /// tidak peduli apakah OS komputer Rumah Sakit diset ke US, UK, atau Indonesia.
        /// </summary>
        public string TanggalLahirFormatted
        {
            get
            {
                if (TanggalLahir == DateTime.MinValue) return "-";
                return TanggalLahir.ToString("dd MMMM yyyy", new CultureInfo("id-ID"));
            }
        }

        // =========================================================================
        // DATA RUMAH SAKIT & DOKTER
        // =========================================================================
        public string Hospital { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Dokter { get; set; } = string.Empty;

        // =========================================================================
        // DATA KLINIS ENDOSKOPI (TEKS)
        // =========================================================================
        public string Keluhan { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string ObatPremedikasi { get; set; } = string.Empty;
        public string Alat { get; set; } = string.Empty;

        // =========================================================================
        // RELASI GAMBAR MEDIS (ANTI-TERTUKAR)
        // =========================================================================
        /// <summary>
        /// Mengikat data gambar endoskopi langsung ke dalam manifes data pasien yang bersangkutan.
        /// </summary>
        public List<MedicalImageModel> FotoEndoskopi { get; set; } = new List<MedicalImageModel>();
    }
}