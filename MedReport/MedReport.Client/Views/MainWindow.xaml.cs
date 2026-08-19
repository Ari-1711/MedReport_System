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

            // Panggil fungsi yang benar dan teruskan nilai API/Mapping yang sudah ada di cache
            bool isSuccess = ConfigService.SaveFullConfiguration(
                name,
                address,
                _hospitalLogoPath ?? "",
                ConfigService.ApiUrl,
                ConfigService.DoctorApiUrl,
                ConfigService.GetMappingValue("NamaKey"),
                ConfigService.GetMappingValue("TglLahirKey"),
                ConfigService.GetMappingValue("GenderKey"),
                ConfigService.GetMappingValue("DoctorNameKey")
            );

            if (isSuccess)
                MessageBox.Show("Template Rumah Sakit berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Gagal menyimpan template.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // =========================================================================
        // MANAGEMENT CONFIGURATION IT RS - 4 MENU SPESIFIK & AMAN
        // =========================================================================
        private void BtnNetworkSetting_Click(object sender, RoutedEventArgs e)
        {
            PinValidationWindow pinWindow = new PinValidationWindow();
            pinWindow.Owner = this;

            if (pinWindow.ShowDialog() == true && pinWindow.IsAuthenticated)
            {
                // Tampilan Menu Utama IT yang Terstruktur dan Profesional
                string menuPrompt = "SISTEM KONFIGURASI SIMRS - MEDREPORT\n\n" +
                                    "Silahkan pilih nomor menu untuk melakukan pembaruan:\n" +
                                    "[1] Perbarui Identitas Rumah Sakit (Nama & Alamat)\n" +
                                    "[2] Perbarui Endpoint Server Jaringan (URL Server API)\n" +
                                    "[3] Perbarui Pemetaan Kolom Data (Dynamic Mapping JSON)\n" +
                                    "[4] Ubah PIN Otorisasi Keamanan IT";

                string pilihanMenu = Microsoft.VisualBasic.Interaction.InputBox(menuPrompt, "Menu Utama Teknisi IT RS", "1");
                if (string.IsNullOrWhiteSpace(pilihanMenu)) return; // Batal jika kosong

                // Ambil data dari cache saat ini sebagai nilai default (Anti-Loss Data)
                string currentHospitalName = ConfigService.HospitalName;
                string currentHospitalAddress = ConfigService.HospitalAddress;
                string currentPatientApi = ConfigService.ApiUrl;
                string currentDoctorApi = ConfigService.GetValue("DoctorApiUrl");
                if (string.IsNullOrWhiteSpace(currentDoctorApi)) currentDoctorApi = "http://localhost:3000/dokter";

                string currentNamaKey = ConfigService.GetMappingValue("NamaKey");
                string currentTglLahirKey = ConfigService.GetMappingValue("TglLahirKey");
                string currentGenderKey = ConfigService.GetMappingValue("GenderKey");
                string currentDoctorNameKey = ConfigService.GetMappingValue("DoctorNameKey");

                bool isConfigChanged = false;

                if (pilihanMenu == "1")
                {
                    // ==========================================
                    // MENU 1: IDENTITAS RUMAH SAKIT
                    // ==========================================
                    string newHospitalName = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Nama Resmi Rumah Sakit:", "Perbarui Identitas RS", currentHospitalName);
                    if (string.IsNullOrEmpty(newHospitalName)) return;

                    string newHospitalAddress = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Alamat Lengkap Rumah Sakit:", "Perbarui Identitas RS", currentHospitalAddress);
                    if (string.IsNullOrEmpty(newHospitalAddress)) return;

                    currentHospitalName = newHospitalName;
                    currentHospitalAddress = newHospitalAddress;
                    isConfigChanged = true;
                }
                else if (pilihanMenu == "2")
                {
                    // ==========================================
                    // MENU 2: ENDPOINT SERVER JARINGAN
                    // ==========================================
                    string newPatientApi = Microsoft.VisualBasic.Interaction.InputBox("Masukkan URL Integrasi Data Rekam Medis Pasien:", "Perbarui Endpoint Jaringan", currentPatientApi);
                    if (string.IsNullOrEmpty(newPatientApi)) return;

                    string newDoctorApi = Microsoft.VisualBasic.Interaction.InputBox("Masukkan URL Sinkronisasi Data Master Dokter:", "Perbarui Endpoint Jaringan", currentDoctorApi);
                    if (string.IsNullOrEmpty(newDoctorApi)) return;

                    currentPatientApi = newPatientApi;
                    currentDoctorApi = newDoctorApi;
                    isConfigChanged = true;
                }
                else if (pilihanMenu == "3")
                {
                    // ==========================================
                    // MENU 3: DYNAMIC MAPPING DATA SIMRS
                    // ==========================================
                    string newNamaKey = Microsoft.VisualBasic.Interaction.InputBox("Key Objek JSON untuk Nama Pasien:", "Perbarui Pemetaan Kolom Data", currentNamaKey);
                    if (string.IsNullOrEmpty(newNamaKey)) return;

                    string newTglLahirKey = Microsoft.VisualBasic.Interaction.InputBox("Key Objek JSON untuk Tanggal Lahir Pasien:", "Perbarui Pemetaan Kolom Data", currentTglLahirKey);
                    if (string.IsNullOrEmpty(newTglLahirKey)) return;

                    string newGenderKey = Microsoft.VisualBasic.Interaction.InputBox("Key Objek JSON untuk Jenis Kelamin Pasien:", "Perbarui Pemetaan Kolom Data", currentGenderKey);
                    if (string.IsNullOrEmpty(newGenderKey)) return;

                    string newDoctorNameKey = Microsoft.VisualBasic.Interaction.InputBox("Key Objek JSON untuk Nama Lengkap Dokter:", "Perbarui Pemetaan Kolom Data", currentDoctorNameKey);
                    if (string.IsNullOrEmpty(newDoctorNameKey)) return;

                    currentNamaKey = newNamaKey;
                    currentTglLahirKey = newTglLahirKey;
                    currentGenderKey = newGenderKey;
                    currentDoctorNameKey = newDoctorNameKey;
                    isConfigChanged = true;
                }
                else if (pilihanMenu == "4")
                {
                    // ==========================================
                    // MENU 4: UBAH PIN KEAMANAN IT
                    // ==========================================
                    string inputPin1 = Microsoft.VisualBasic.Interaction.InputBox("Masukkan PIN Baru Otorisasi IT (Hanya Angka):", "Ubah Akses PIN Keamanan", "");
                    if (string.IsNullOrWhiteSpace(inputPin1)) return;

                    string inputPin2 = Microsoft.VisualBasic.Interaction.InputBox("Konfirmasi Ulang PIN Baru Anda:", "Ubah Akses PIN Keamanan", "");

                    if (inputPin1 != inputPin2)
                    {
                        MessageBox.Show("Validasi Gagal! PIN Baru dan Konfirmasi tidak cocok.", "Error Akses", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    using (System.Security.SecureString secureNewPin = new System.Security.SecureString())
                    {
                        foreach (char c in inputPin1) secureNewPin.AppendChar(c);
                        secureNewPin.MakeReadOnly();

                        if (ConfigService.ChangeItPin(secureNewPin))
                        {
                            MessageBox.Show("PIN Keamanan Akses IT Berhasil Diperbarui!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Gagal mengamankan berkas enkripsi PIN baru ke disk.", "Sistem Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    return; // Keluar fungsi karena PIN terpisah dari berkas SaveFullConfiguration umum
                }
                else
                {
                    MessageBox.Show("Pilihan menu tidak valid.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // EKSEKUSI PENYIMPANAN DATA (Jika terjadi perubahan pada Menu 1, 2, atau 3)
                if (isConfigChanged)
                {
                    try
                    {
                        bool isSuccess = ConfigService.SaveFullConfiguration(
                            currentHospitalName, currentHospitalAddress, _hospitalLogoPath,
                            currentPatientApi, currentDoctorApi,
                            currentNamaKey, currentTglLahirKey, currentGenderKey, currentDoctorNameKey
                        );

                        if (isSuccess)
                        {
                            MessageBox.Show("Pembaruan Konfigurasi SIMRS Berhasil Disimpan!", "Sukses Terkonfigurasi", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Sinkronisasi Reaktif ke UI Suster melalui ViewModel
                            if (FormPasien.DataContext is ViewModels.PatientFormViewModel patientVm)
                            {
                                patientVm.RefreshHospitalData();
                                _ = patientVm.MuatDaftarDokterAsync(); // Tarik ulang master dokter di background jika server dirubah
                            }
                        }
                        else
                        {
                            MessageBox.Show("Gagal menulis pembaruan konfigurasi ke disk lokal terenkripsi.", "Sistem Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Terjadi kegagalan sistem saat menyimpan konfigurasi: {ex.Message}", "Sistem Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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