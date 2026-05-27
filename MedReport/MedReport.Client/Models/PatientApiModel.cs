using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MedReport.Client.Models
{
    /// <summary>
    /// Merepresentasikan model data pasien yang digunakan untuk berinteraksi dengan API Rumah Sakit.
    /// </summary>
    public class PatientApiModel
    {
        /// <summary>
        /// Nomor rekam medis atau identitas unik pasien.
        /// </summary>
        public string IdPasien { get; set; } = string.Empty;

        /// <summary>
        /// Nama lengkap pasien.
        /// </summary>
        public string Nama { get; set; } = string.Empty;

        /// <summary>
        /// Tanggal lahir pasien.
        /// </summary>
        public DateTime? TanggalLahir { get; set; }

        /// <summary>
        /// Jenis kelamin pasien (misal: "L", "P", "Laki-laki", atau "Perempuan").
        /// </summary>
        public string Gender { get; set; } = string.Empty;
    }
}