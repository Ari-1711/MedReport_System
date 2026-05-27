using System;
using System.Collections.Generic;
using System.Text;

namespace MedReport.Client.Models
{
    internal class MedicalImageModel
    {
        // Menyimpan lokasi gambar asli (High Res) untuk Report PDF
        public string OriginalPath { get; set; }
        // Menyimpan gambar kecil (Low Res) untuk UI
        public System.Windows.Media.Imaging.BitmapImage Thumbnail { get; set; }
    }
}
