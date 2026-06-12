using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using MedReport.Client.Services;
using MedReport.Client.Views; // SOLUSI EROR 1: Namespace Views wajib dimasukkan eksplisit

namespace MedReport.Client
{
    public partial class MainWindow : Window
    {
        private string _hospitalLogoPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            MuatLogoTersimpan();
        }

        private void MuatLogoTersimpan()
        {
            try
            {
                string savedLogoPath = ConfigService.HospitalLogoPath;
                if (!string.IsNullOrWhiteSpace(savedLogoPath) && File.Exists(savedLogoPath))
                {
                    _hospitalLogoPath = savedLogoPath;
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
            catch
            {
                LogoContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void tbnmedicalapp_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult userResponse = MessageBox.Show("Apakah Anda yakin ingin keluar?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (userResponse == MessageBoxResult.No) e.Cancel = true;
        }

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

        private void BtnRemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            HospitalLogo.Source = null;
            _hospitalLogoPath = string.Empty;
            LogoContainer.Visibility = Visibility.Collapsed;
        }

        private byte[]? GetBytesFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return null;
            try { return File.ReadAllBytes(filePath); } catch { return null; }
        }

        private void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            string name = FormPasien.TxtRs.Text.Trim();
            string address = FormPasien.TxtAlamatRs.Text.Trim();
            if (ConfigService.SaveTemplate(name, address, _hospitalLogoPath ?? ""))
                MessageBox.Show("Template Rumah Sakit berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Gagal menyimpan template.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // =========================================================================
        // FITUR BARU: MANAGEMENT CONFIGURATION JARINGAN IT (TERPROTEKSI PIN)
        // =========================================================================
        private void BtnNetworkSetting_Click(object sender, RoutedEventArgs e)
        {
            PinValidationWindow pinWindow = new PinValidationWindow();
            pinWindow.Owner = this;

            if (pinWindow.ShowDialog() == true && pinWindow.IsAuthenticated)
            {
                string currentPatientApi = ConfigService.ApiUrl;
                string currentDoctorApi = ConfigService.GetValue("DoctorApiUrl");
                if (string.IsNullOrWhiteSpace(currentDoctorApi)) currentDoctorApi = "http://localhost:3000/dokter";

                string newPatientApi = Microsoft.VisualBasic.Interaction.InputBox("Masukkan URL Server API Rekam Medis Pasien yang baru:", "Konfigurasi API Pasien", currentPatientApi);
                if (string.IsNullOrEmpty(newPatientApi)) return;

                string newDoctorApi = Microsoft.VisualBasic.Interaction.InputBox("Masukkan URL Server API Master Daftar Dokter yang baru:", "Konfigurasi API Dokter", currentDoctorApi);
                if (string.IsNullOrEmpty(newDoctorApi)) return;

                // SOLUSI EROR 2: Amankan runtime instance pemanggilan SaveTemplate dari data null pointer
                try
                {
                    string hospitalName = ConfigService.HospitalName ?? string.Empty;
                    string hospitalAddress = ConfigService.HospitalAddress ?? string.Empty;

                    // Tulis ulang struktur data lama dengan menyisipkan alamat API modifikasi baru
                    ConfigService.SaveTemplate(hospitalName, hospitalAddress, _hospitalLogoPath);

                    MessageBox.Show(
                        "Konfigurasi alamat Jaringan Server RS Berhasil Diperbarui!\n\n" +
                        "Sistem mendeteksi pembaruan endpoint baru. Harap restart aplikasi.",
                        "Sukses Terkonfigurasi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal menyimpan konfigurasi jaringan baru: {ex.Message}", "Sistem Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
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

            PgbLoading.Visibility = Visibility.Visible;
            BtnGenerate.IsEnabled = false;

            try
            {
                byte[]? gambarTandaTanganBytes = TandaTangan.GetSignatureImage();
                byte[]? logoBytes = GetBytesFromFile(_hospitalLogoPath);
                dataPasien.FotoEndoskopi = GaleriFoto.SelectedPhotos.Select(p => p.DataModel).ToList();

                await ReportService.GenerateAsync(dataPasien, gambarTandaTanganBytes, logoBytes);
                MessageBox.Show("Laporan PDF berhasil dibuat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

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
                PgbLoading.Visibility = Visibility.Collapsed;
                BtnGenerate.IsEnabled = true;
            }
        }
    }
}