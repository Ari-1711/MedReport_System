using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using MedReport.Client.Models;

namespace MedReport.Client.Views
{
    public partial class SignatureView : UserControl
    {
        // -------------------------------------------------------------------------
        // CONSTRUCTOR: Dipanggil saat UserControl Tanda Tangan dimuat di layar
        // -------------------------------------------------------------------------
        public SignatureView()
        {
            InitializeComponent();
        }

        // -------------------------------------------------------------------------
        // FUNGSI RESET GLOBAL: Pembersihan Pasca-Cetak
        // -------------------------------------------------------------------------
        public void ResetTandaTangan()
        {
            Sigpad?.Strokes?.Clear();
            TxtAnamnesa.Clear();
            TxtHasil.Clear();
            TxtKesimpulan.Clear();
            TxtSaran.Clear();
        }

        // -------------------------------------------------------------------------
        // FUNGSI RESET LOKAL: Tombol UI "Clear"
        // -------------------------------------------------------------------------
        private void BtnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            Sigpad?.Strokes?.Clear();
        }

        // -------------------------------------------------------------------------
        // FUNGSI EKSTRAKSI DATA: Menarik teks dari layar untuk QuestPDF
        // -------------------------------------------------------------------------
        public (string Anamnesa, string Hasil, string Kesimpulan, string Saran) GetProcedureText()
        {
            return (
                TxtAnamnesa.Text?.Trim() ?? string.Empty,
                TxtHasil.Text?.Trim() ?? string.Empty,
                TxtKesimpulan.Text?.Trim() ?? string.Empty,
                TxtSaran.Text?.Trim() ?? string.Empty
            );
        }

        // -------------------------------------------------------------------------
        // CORE RENDER ENGINE: Dioptimalkan dengan High-DPI untuk Ketajaman Cetak
        // -------------------------------------------------------------------------
        public byte[]? GetSignatureImage()
        {
            // Fallback Lapis 1: Jika dokter tidak tanda tangan (memilih tanda tangan basah),
            // kembalikan null secara aman agar PDF tahu harus mengosongkan space.
            if (Sigpad == null || Sigpad.Strokes.Count == 0) return null;

            // MENGHINDARI CRASH DIMENSI (DPI SCALING)
            double actualWidth = Sigpad.ActualWidth;
            double actualHeight = Sigpad.ActualHeight;

            // Fallback Lapis 2: Mencegah ArgumentException jika UI belum selesai me-render komponen.
            if (actualWidth <= 0 || actualHeight <= 0) return null;

            try
            {
                // IMPLEMENTASI OPTIMASI 3: HIGH-DPI SCALING FACTOR (3.0x)
                // Kita kalikan resolusi render sebanyak 3x lipat agar garis tanda tangan tidak blur saat masuk PDF.
                double scaleFactor = 3.0;

                int renderWidth = (int)Math.Ceiling(actualWidth * scaleFactor);
                int renderHeight = (int)Math.Ceiling(actualHeight * scaleFactor);

                // Gunakan koordinat DPI yang dinaikkan (96 * 3 = 288 DPI) untuk kualitas cetak printer laser.
                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    renderWidth,
                    renderHeight,
                    96d * scaleFactor,
                    96d * scaleFactor,
                    PixelFormats.Default);

                rtb.Render(Sigpad);

                // ENCODING KE PNG (Mendukung transparansi latar belakang)
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                // MANAJEMEN MEMORI KETAT (MemoryStream)
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    encoder.Save(ms);
                    return ms.ToArray(); // Kirim biner tajam ke pembuat PDF
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal merender tanda tangan: {ex.Message}");
                return null;
            }
        }
    }
}