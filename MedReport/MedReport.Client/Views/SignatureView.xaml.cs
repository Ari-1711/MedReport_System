using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MedReport.Client.Models;

namespace MedReport.Client.Views
{
    public partial class SignatureView : UserControl
    {
        public SignatureView()
        {
            InitializeComponent();
        }

        public void ResetTandaTangan()
        {
            Sigpad?.Strokes?.Clear();
            TxtAnamnesa.Clear();
            TxtHasil.Clear();
            TxtKesimpulan.Clear();
            TxtSaran.Clear();
        }

        private void BtnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            Sigpad?.Strokes?.Clear();
        }

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
        // CORE RENDER ENGINE: Terproteksi dari Kegagalan Format Piksel & Blind Spot Legal
        // -------------------------------------------------------------------------
        public byte[]? GetSignatureImage()
        {
            // Jika pad tanda tangan kosong, kembalikan null secara legal (Suster/Dokter memilih tanda tangan basah)
            if (Sigpad == null || Sigpad.Strokes.Count == 0) return null;

            double actualWidth = Sigpad.ActualWidth;
            double actualHeight = Sigpad.ActualHeight;

            if (actualWidth <= 0 || actualHeight <= 0) return null;

            try
            {
                double scaleFactor = 3.0;
                int renderWidth = (int)Math.Ceiling(actualWidth * scaleFactor);
                int renderHeight = (int)Math.Ceiling(actualHeight * scaleFactor);

                // SOLUSI AUDIT KUTU VISUAL: Kunci format piksel ke Pbgra32. 
                // Format ini mengunci komponen Alpha Channel secara konstan agar latar belakang coretan 
                // dipastikan 100% transparan dan bersih di atas kertas PDF, bebas dari bug kotak hitam.
                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    renderWidth,
                    renderHeight,
                    96d * scaleFactor,
                    96d * scaleFactor,
                    PixelFormats.Pbgra32);

                rtb.Render(Sigpad);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                // SOLUSI AUDIT ASPEK LEGAL: Jangan bungkam eror grafis! 
                // Lempar exception bermakna agar MainWindow tahu proses konversi gagal akibat masalah sistem,
                // sehingga aplikasi memblokir penerbitan laporan PDF ilegal tanpa tanda tangan.
                throw new InvalidOperationException(
                    "Sistem gagal melakukan konversi digital tanda tangan dokter akibat keterbatasan memori grafis komputer.\n\n" +
                    $"Detail Kegagalan GDI+: {ex.Message}");
            }
        }
    }
}