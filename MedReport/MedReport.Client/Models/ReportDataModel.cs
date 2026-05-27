using System;
using System.Collections.Generic;
using System.Text;

namespace MedReport.Client.Models
{
    public class ReportDataModel
    {
        // 1. Data Pasien (Mengambil dari PatientApiModel)
        // Ini lebih efektif daripada menulis Nama, Gender, dll satu per satu
        public PatientApiModel Patient { get; set; } = new();

        // 2. Data Rumah Sakit & Dokter
        public string Hospital { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Dokter { get; set; } = string.Empty;

        // 3. Data Klinis Endoskopi
        public string Keluhan { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string ObatPremedikasi { get; set; } = string.Empty;
        public string Alat { get; set; } = string.Empty;

        // 4. Daftar Foto (Menggunakan List dari MedicalImageModel)
        // Otomatis bisa menampung banyak foto sekaligus
        public List<MedicalImageModel> Images { get; set; } = new();

        // 5. Metadata Laporan
        public DateTime TanggalLaporan { get; set; } = DateTime.Now;
        public string SignaturePath { get; set; } = string.Empty;
    }
}
