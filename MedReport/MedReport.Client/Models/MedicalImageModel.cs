using System;

namespace MedReport.Client.Models
{
    /// <summary>
    /// Model Data murni untuk representasi gambar medis.
    /// Kelas ini bebas dari dependensi UI (WPF) untuk mencegah kebocoran memori dan file locking.
    /// </summary>
    public class MedicalImageModel
    {
        // 1. Menyimpan lokasi fisik gambar asli (High Res) di Harddisk
        public string OriginalPath { get; set; } = string.Empty;

        // 2. Properti opsional untuk penanda unik data (Id Gambar)
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}