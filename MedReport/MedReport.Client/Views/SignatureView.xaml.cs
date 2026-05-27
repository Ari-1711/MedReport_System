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
        // Dipanggil dari MainWindow HANYA setelah laporan PDF sukses dibuat.
        // Membersihkan seluruh catatan medis dan kanvas untuk pasien berikutnya.
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
        // Hanya menghapus coretan tanda tangan jika dokter salah tanda tangan,
        // tanpa menghapus teks catatan medis yang sudah diketik panjang lebar.
        // -------------------------------------------------------------------------
        private void BtnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            Sigpad?.Strokes?.Clear();
        }

        // -------------------------------------------------------------------------
        // FUNGSI EKSTRAKSI DATA: Menarik teks dari layar untuk QuestPDF
        // Menggunakan Tuple (string, string, string, string) agar lebih ringkas 
        // tanpa harus membuat class Model terpisah khusus untuk 4 variabel ini.
        // -------------------------------------------------------------------------
        public (string Anamnesa, string Hasil, string Kesimpulan, string Saran) GetProcedureText()
        {
            // .Trim() SANGAT KRUSIAL DI SINI. 
            // Jika dokter tidak sengaja menekan 'Spasi' atau 'Enter' berlebih di akhir kalimat,
            // .Trim() akan memotongnya sehingga layout PDF (QuestPDF) tidak berantakan/tergeser.
            return (
                TxtAnamnesa.Text?.Trim() ?? string.Empty,
                TxtHasil.Text?.Trim() ?? string.Empty,
                TxtKesimpulan.Text?.Trim() ?? string.Empty,
                TxtSaran.Text?.Trim() ?? string.Empty
            );
        }

        // -------------------------------------------------------------------------
        // CORE RENDER ENGINE: Mengubah coretan vektor (InkCanvas) menjadi gambar biner (PNG)
        // -------------------------------------------------------------------------
        public byte[]? GetSignatureImage()
        {
            // Validasi Lapis 1: Jika kanvas kosong, kembalikan null. 
            // Jangan buang memori untuk merender gambar putih kosong.
            if (Sigpad.Strokes.Count == 0) return null;

            // MENGHINDARI CRASH DIMENSI (DPI SCALING)
            // Windows UI sering menggunakan skala desimal (misal 150.5 px) pada layar beresolusi tinggi.
            // RenderTargetBitmap hanya menerima angka bulat (Integer). 
            // Math.Ceiling membulatkan ke atas agar gambar tidak terpotong atau memicu Exception.
            int width = (int)Math.Ceiling(Sigpad.ActualWidth);
            int height = (int)Math.Ceiling(Sigpad.ActualHeight);

            // Validasi Lapis 2: Mencegah ArgumentException.
            // Jika fungsi ini terpicu sebelum UI WPF selesai merender kanvas di layar (width/height = 0),
            // batalkan proses agar aplikasi tidak force close.
            if (width <= 0 || height <= 0) return null;

            // PROSES RENDER BITMAP
            // Resolusi default adalah 96 DPI (Standard Monitor).
            // Catatan: Jika saat di-print di kertas HVS hasilnya pecah/buram, 
            // naikkan nilai 96d, 96d ini menjadi 144d, 144d atau 192d, 192d.
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
            rtb.Render(Sigpad);

            // ENCODING KE PNG (Mendukung transparansi latar belakang / Alpha Channel)
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            // MANAJEMEN MEMORI KETAT (MemoryStream)
            // Menggunakan blok 'using' agar aliran memori (MemoryStream) langsung dihancurkan 
            // dan dikembalikan ke RAM PC sesaat setelah byte array berhasil diekstrak.
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray(); // Kirim biner mentah ke pembuat PDF
            }
        }
    }
}