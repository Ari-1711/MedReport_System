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
            // Jalur absolut agar aman meski dipanggil dari Shortcut Windows
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string jsonString = File.ReadAllText(configPath);
                    var config = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
                    string savedLogoPath = config?["HospitalLogoPath"]?.ToString() ?? "";

                    // Validasi: Pastikan path ada DAN file gambar fisiknya belum dihapus oleh user
                    if (!string.IsNullOrWhiteSpace(savedLogoPath) && File.Exists(savedLogoPath))
                    {
                        _hospitalLogoPath = savedLogoPath;

                        // Tampilkan ke UI dengan manajemen memori ketat (BitmapCacheOption.OnLoad)
                        // Ini mencegah file gambar terkunci oleh sistem sehingga bisa dihapus/diganti nanti
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(_hospitalLogoPath);
                        bitmap.DecodePixelWidth = 200; // Perkecil resolusi di RAM agar aplikasi tidak berat
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        // Freeze() membuat gambar menjadi Read-Only, menghemat beban CPU & RAM di WPF
                        if (bitmap.CanFreeze) bitmap.Freeze();

                        HospitalLogo.Source = bitmap;
                        LogoContainer.Visibility = Visibility.Visible;
                    }
                }
                catch
                {
                    // Abaikan jika json rusak, biarkan logo kosong
                }
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
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            if (!System.IO.File.Exists(configPath)) return;

            try
            {
                // 1. Baca isi config saat ini
                string jsonString = System.IO.File.ReadAllText(configPath);
                var config = System.Text.Json.Nodes.JsonNode.Parse(jsonString);

                // 2. Tarik data dari elemen anak (FormPasien)
                config["HospitalName"] = FormPasien.TxtRs.Text.Trim();
                config["HospitalAddress"] = FormPasien.TxtAlamatRs.Text.Trim();

                // 3. Simpan jalur logo
                config["HospitalLogoPath"] = _hospitalLogoPath ?? "";

                // 4. Timpa file lama dengan data baru
                System.IO.File.WriteAllText(configPath, config.ToString());

                MessageBox.Show("Template Rumah Sakit berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                foreach (var modelGambar in GaleriFoto.SelectedPhotos)
                {
                    byte[]? fileBiner = GetBytesFromFile(modelGambar.OriginalPath);
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