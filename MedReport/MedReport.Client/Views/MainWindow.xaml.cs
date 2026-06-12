using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
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
                string savedLogoPath = ConfigService.HospitalLogoPath;

                if (!string.IsNullOrWhiteSpace(savedLogoPath) && File.Exists(savedLogoPath))
                {
                    _hospitalLogoPath = savedLogoPath;

                    // TAMPILKAN KE UI DENGAN PROSES PENYALINAN RAM YANG TEPAT (ANTI-LOCK)
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_hospitalLogoPath);
                    bitmap.DecodePixelWidth = 200;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Melepas file fisik setelah dibaca
                    bitmap.EndInit();

                    if (bitmap.CanFreeze) bitmap.Freeze();

                    HospitalLogo.Source = bitmap;
                    LogoContainer.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                LogoContainer.Visibility = Visibility.Collapsed;
            }
        }

        // -------------------------------------------------------------------------
        // EVENT LISTENER: Mencegah aplikasi tertutup tanpa sengaja (Safety Net)
        // -------------------------------------------------------------------------
        private void tbnmedicalapp_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult userResponse = MessageBox.Show(
                "Apakah Anda yakin ingin keluar?",
                "Konfirmasi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

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

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_hospitalLogoPath);
                bitmap.DecodePixelWidth = 200;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                if (bitmap.CanFreeze) bitmap.Freeze();

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
        // Menggunakan blok 'using' secara implisit untuk menjamin pelepasan handle file
        // -------------------------------------------------------------------------
        private byte[]? GetBytesFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                // File.ReadAllBytes secara internal mengelola open, read, dan close stream secara aman
                return File.ReadAllBytes(filePath);
            }
            catch
            {
                return null;
            }
        }

        // -------------------------------------------------------------------------
        // EVENT LISTENER: Menyimpan nama, alamat, dan logo RS ke config.json
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
        // CORE ENGINE: Mengeksekusi pembuatan laporan PDFSecara Asinkronus
        // -------------------------------------------------------------------------
        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // --- TAHAP 1: VALIDASI MEDIS AWAL ---
            var dataPasien = FormPasien.GetPatientData();
            if (string.IsNullOrWhiteSpace(dataPasien.Nama) || string.IsNullOrWhiteSpace(dataPasien.IdPasien))
            {
                MessageBox.Show("Nama Pasien and ID Pasien wajib diisi sebelum membuat laporan.", "Validasi Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (GaleriFoto.SelectedPhotos == null || GaleriFoto.SelectedPhotos.Count == 0)
            {
                MessageBox.Show("Harap pilih minimal 1 foto endoskopi sebelum membuat laporan.", "Validasi Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- TAHAP 2: KUNCI UI ---
            PgbLoading.Visibility = Visibility.Visible;
            BtnGenerate.IsEnabled = false;

            try
            {
                // --- TAHAP 3: EKSTRAKSI DATA UI (Wajib dilakukan di UI Thread) ---
                byte[]? gambarTandaTanganBytes = TandaTangan.GetSignatureImage();
                byte[]? logoBytes = GetBytesFromFile(_hospitalLogoPath);

                // Sinkronisasi data model murni dari wrapper UI galeri ke dalam manifes laporan
                dataPasien.FotoEndoskopi = GaleriFoto.SelectedPhotos.Select(p => p.DataModel).ToList();

                // --- TAHAP 4: EKSEKUSI PIPELINE GENERATOR (NON-BLOCKING ASYNC) ---
                // Menjalankan tugas pembuatan PDF asinkronus tanpa membuat UI hang mendadak
                await ReportService.GenerateAsync(dataPasien, gambarTandaTanganBytes, logoBytes);

                // Notifikasi sukses
                MessageBox.Show("Laporan PDF berhasil dibuat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

                // Bersihkan form rekam medis secara sadar demi keamanan data pasien berikutnya
                FormPasien.ResetFormPasien();
                GaleriFoto.ResetGaleri();
                TandaTangan.ResetTandaTangan();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memproses laporan: {ex.Message}", "Kesalahan Sistem Laporan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // --- TAHAP 5: KEMBALIKAN STATE UI & PELEPASAN REFERENSI ---
                // GC .NET akan membersihkan alokasi memori secara otomatis dan cerdas saat 
                // variabel lokal di dalam metode ini keluar dari ruang lingkup cakupan (out of scope).
                PgbLoading.Visibility = Visibility.Collapsed;
                BtnGenerate.IsEnabled = true;
            }
        }
    }
}