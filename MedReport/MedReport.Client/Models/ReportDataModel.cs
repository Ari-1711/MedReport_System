using System;
using System.Collections.Generic;
using System.Text;

namespace MedReport.Client.Models
{
    public class ReportDataModel
    {
        // Data Demografi (Bisa dari API)
        public string IdPasien { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string TanggalLahir { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;

        // Data Rumah Sakit & Dokter
        public string Hospital { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Dokter { get; set; } = string.Empty;

        // Data Klinis Endoskopi
        public string Keluhan { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string ObatPremedikasi { get; set; } = string.Empty;
        public string Alat { get; set; } = string.Empty;
    }
}
