using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using MedReport.Client.Services;


namespace MedReport.Client
{
    public partial class MainWindow : Window
    {
        // Variabel global untuk menyimpan lokasi file logo rumah sakit
        private string _hospitalLogoPath = string.Empty;

        // -------------------------------------------------------------------------
        // CONSTRUCTOR: Dipanggil saat aplikasi pertama kali menyala
        // -------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();
            MuatLogoTersimpan(); // Kembalikan logo RS terakhir yang dipakai
        }

        // -------------------------------------------------------------------------
        // FUNGSI INIT: Menarik logo rumah sakit dari penyimpanan (config.json)
        // -------------------------------------------------------------------------
        private void MuatLogoTersimpan()
        {
            try
            {
                // 1. AMBIL LANGSUNG DARI RAM (ConfigService sudah memuat file saat Startup)
                // Kita tidak perlu lagi memanggil File.ReadAllText manual di sini.
                string savedLogoPath = ConfigService.HospitalLogoPath;

                // 2. VALIDASI FISIK FILE
                if (!string.IsNullOrWhiteSpace(savedLogoPath) && File.Exists(savedLogoPath))
                {
                    _hospitalLogoPath = savedLogoPath;

                    // 3. TAMPILKAN KE UI
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_hospitalLogoPath);
                    bitmap.DecodePixelWidth = 200;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Penting: Agar file tidak terkunci
                    bitmap.EndInit();

                    if (bitmap.CanFreeze) bitmap.Freeze();

                    HospitalLogo.Source = bitmap;
                    LogoContainer.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // Jika logo gagal dimuat (misal file gambar korup), sembunyikan container
                LogoContainer.Visibility = Visibility.Collapsed;
            }
        }

        // -------------------------------------------------------------------------
        // EVENT LISTENER: Mencegah aplikasi tertutup tanpa sengaja (Safety Net)
        // -------------------------------------------------------------------------
        private void tbnmedicalapp_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult userResponse = MessageBox.Show("Apakah Anda yakin ingin keluar?",
                 "Konfirmasi",
                 MessageBoxButton.YesNo,
                 MessageBoxImage.Warning,
                 MessageBoxResult.No);

            // Jika suster tidak sengaja menekan tombol silang (X), batalkan proses penutupan (Cancel = true)
            if (userResponse == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI UI: Mengimpor logo baru melalui File Explorer Windows
        // -------------------------------------------------------------------------
        private void BtnImportLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                _hospitalLogoPath = openFileDialog.FileName;

                // Proses render ulang logo baru ke layar dengan optimasi memori yang sama
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_hospitalLogoPath);
                bitmap.DecodePixelWidth = 200;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                if (bitmap.CanFreeze)
                {
                    bitmap.Freeze();
                }

                HospitalLogo.Source = bitmap;
                LogoContainer.Visibility = Visibility.Visible;
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI UI: Menghapus logo dari layar dan memori
        // -------------------------------------------------------------------------
        private void BtnRemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            HospitalLogo.Source = null;
            _hospitalLogoPath = string.Empty;
            LogoContainer.Visibility = Visibility.Collapsed;
        }

        // -------------------------------------------------------------------------
        // HELPER UTILITY: Mengubah file gambar di Harddisk menjadi kode Biner (byte[])
        // Jauh lebih aman memproses byte[] langsung dari disk daripada mengekstraknya dari UI (Image Control)
        // -------------------------------------------------------------------------
        private byte[]? GetBytesFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch
            {
                return null;
            }
        }

        // -------------------------------------------------------------------------
        // EVENT LISTENER: Menyimpan nama, alamat, dan logo RS ke config.json
        // Ini adalah fitur "Template" agar perawat tidak perlu mengetik ulang nama RS setiap hari
        // -------------------------------------------------------------------------
        private void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            string name = FormPasien.TxtRs.Text.Trim();
            string address = FormPasien.TxtAlamatRs.Text.Trim();

            if (ConfigService.SaveTemplate(name, address, _hospitalLogoPath ?? ""))
            {
                MessageBox.Show("Template Rumah Sakit berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Gagal menyimpan template.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------------------------------------------------------
        // CORE ENGINE: Mengeksekusi pembuatan laporan PDF (Proses Paling Krusial)
        // -------------------------------------------------------------------------
        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // --- TAHAP 1: VALIDASI MEDIS AWAL (Idiot-Proof) ---
            // Cegah pembuatan laporan jika data inti kosong. Laporan medis tanpa ID/Nama = Malapraktik
            var dataPasien = FormPasien.GetPatientData();
            if (string.IsNullOrWhiteSpace(dataPasien.Nama) || string.IsNullOrWhiteSpace(dataPasien.IdPasien))
            {
                MessageBox.Show("Nama Pasien dan ID Pasien wajib diisi sebelum membuat laporan.", "Validasi Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validasi keberadaan foto endoskopi
            if (GaleriFoto.SelectedPhotos == null || GaleriFoto.SelectedPhotos.Count == 0)
            {
                MessageBox.Show("Harap pilih minimal 1 foto endoskopi sebelum membuat laporan.", "Validasi Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- TAHAP 2: KUNCI UI ---
            // Munculkan indikator loading dan matikan tombol agar user tidak klik 2x (mencegah crash)
            PgbLoading.Visibility = Visibility.Visible;
            BtnGenerate.IsEnabled = false;

            // Deklarasi array biner di luar blok try agar memori bisa dihancurkan di blok finally
            List<byte[]?> kumpulanFotoBytes = null;
            byte[]? logoBytes = null;
            byte[]? gambarTandaTanganBytes = null;

            try
            {
                // --- TAHAP 3: EKSTRAKSI DATA UI (Wajib dilakukan di UI Thread) ---
                var catatanPemeriksaan = TandaTangan.GetProcedureText();
                gambarTandaTanganBytes = TandaTangan.GetSignatureImage();
                logoBytes = GetBytesFromFile(_hospitalLogoPath);

                // Ubah semua foto di galeri menjadi biner sebelum dilempar ke mesin PDF
                kumpulanFotoBytes = new List<byte[]?>();
                foreach (var uiModel in GaleriFoto.SelectedPhotos)
                {
                    // Ambil OriginalPath dari DataModel internalnya
                    byte[]? fileBiner = GetBytesFromFile(uiModel.DataModel.OriginalPath);
                    if (fileBiner != null) kumpulanFotoBytes.Add(fileBiner);
                }

                // --- TAHAP 4: PROSES BERAT DI BACKGROUND THREAD ---
                // Melempar tugas QuestPDF ke thread terpisah agar aplikasi tidak "Not Responding"
                await Task.Run(() =>
                {
                    ReportService.Generate(dataPasien, catatanPemeriksaan, kumpulanFotoBytes, gambarTandaTanganBytes, logoBytes);
                });

                // Notifikasi sukses
                MessageBox.Show("Laporan PDF berhasil dibuat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

                // Mencegah Kontaminasi Data Medis dengan membersihkan form otomatis untuk pasien berikutnya
                FormPasien.ResetFormPasien();
                GaleriFoto.ResetGaleri();
                TandaTangan.ResetTandaTangan();
            }
            catch (Exception ex)
            {
                // Tangkapan error global jika rendering PDF gagal atau file sedang dibuka di aplikasi lain
                MessageBox.Show($"Gagal memproses laporan: {ex.Message}", "Kesalahan Sistem Laporan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // --- TAHAP 5: KEMBALIKAN STATE UI ---
                PgbLoading.Visibility = Visibility.Collapsed;
                BtnGenerate.IsEnabled = true;

                // --- TAHAP 6: PEMBERSIHAN MEMORI EKSTREM ---
                // Langkah vital: Memutus referensi memori dari file gambar resolusi tinggi
                // Jika ini dilewati, aplikasi akan memakan RAM hingga crash setelah membuat puluhan laporan
                if (kumpulanFotoBytes != null)
                {
                    kumpulanFotoBytes.Clear();
                    kumpulanFotoBytes = null;
                }
                logoBytes = null;
                gambarTandaTanganBytes = null;

                // Paksa Garbage Collector (Sistem Pembuang Sampah Windows) untuk segera membersihkan RAM
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}