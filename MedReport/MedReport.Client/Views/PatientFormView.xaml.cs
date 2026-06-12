using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;
using MedReport.Client.Models;
using MedReport.Client.Services;
using System.Net.Http;

namespace MedReport.Client.Views
{
    public partial class PatientFormView : UserControl
    {
        private readonly HospitalApiService _apiService = new HospitalApiService();

        // SOLUSI SOCKET EXHAUSTION: Gunakan satu instans HttpClient statis bersama untuk internal form
        private static readonly HttpClient _sharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private bool _isSearching = false;

        public ObservableCollection<MedicalImageUiModel> SelectedPhotos { get; set; }

        public PatientFormView()
        {
            InitializeComponent();
            MuatTemplateRs();
            MuatDaftarDokter();
        }

        private void MuatTemplateRs()
        {
            TxtRs.Text = ConfigService.HospitalName;
            TxtAlamatRs.Text = ConfigService.HospitalAddress;
        }

        private async void MuatDaftarDokter()
        {
            try
            {
                string apiUrlDokter = ConfigService.GetValue("DoctorApiUrl");
                if (string.IsNullOrWhiteSpace(apiUrlDokter))
                {
                    apiUrlDokter = "http://localhost:3000/dokter";
                }

                string keyNamaDokter = ConfigService.GetMappingValue("DoctorNameKey");
                if (string.IsNullOrWhiteSpace(keyNamaDokter))
                {
                    keyNamaDokter = "nama";
                }

                // Menggunakan _sharedClient statis, bukan instansiasi 'new' berulang
                string response = await _sharedClient.GetStringAsync(apiUrlDokter);
                var dokterList = System.Text.Json.Nodes.JsonNode.Parse(response)?.AsArray();

                CmbDokter.Items.Clear();
                if (dokterList != null)
                {
                    foreach (var dok in dokterList)
                    {
                        CmbDokter.Items.Add(dok[keyNamaDokter]?.ToString());
                    }
                    if (CmbDokter.Items.Count > 0) CmbDokter.SelectedIndex = 0;
                }
            }
            catch
            {
                // UI tetap aman jika server data master dokter down
            }
        }

        // =========================================================================
        // VALIDASI INPUT EKSTREM (ANTI-MALAPRAKTIK DATA)
        // =========================================================================

        // Hanya izinkan huruf, spasi, titik, koma, dan apostrof untuk nama medis resmi
        private void NameValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^a-zA-Z\s\.\,\']");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NameTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex(@"[^a-zA-Z\s\.\,\']");
                if (regex.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        // Amankan ID Pasien dari karakter liar clipboard
        private void IdTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                // ID Pasien hanya boleh berisi Alfanumerik dan tanda hubung standar (-)
                Regex regex = new Regex(@"[^a-zA-Z0-9\-]");
                if (regex.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private async void TxtIdPasien_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_isSearching) return;

                string idInput = TxtIdPasien.Text.Trim();
                if (string.IsNullOrEmpty(idInput)) return;

                try
                {
                    _isSearching = true;
                    TxtIdPasien.IsEnabled = false;

                    var hasil = await _apiService.CariPasienAsync(idInput);

                    if (hasil != null)
                    {
                        TxtNama.Text = hasil.Nama;

                        if (hasil.TanggalLahir == DateTime.MinValue)
                            DpTanggalLahir.SelectedDate = null;
                        else
                            DpTanggalLahir.SelectedDate = hasil.TanggalLahir;

                        if (!string.IsNullOrEmpty(hasil.RawGender))
                        {
                            string targetGenderText = hasil.NormalizedGender == GenderType.LakiLaki ? "Laki-laki" : "Perempuan";

                            foreach (ComboBoxItem item in CmbGender.Items)
                            {
                                string itemText = item.Content.ToString();
                                if (itemText.Equals(targetGenderText, StringComparison.OrdinalIgnoreCase) ||
                                    itemText.StartsWith(hasil.RawGender, StringComparison.OrdinalIgnoreCase))
                                {
                                    CmbGender.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Pasien tidak ditemukan.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtNama.Clear();
                        DpTanggalLahir.SelectedDate = null;
                        CmbGender.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Terjadi gangguan koneksi ke server: {ex.Message}", "Gangguan Jaringan", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _isSearching = false;
                    TxtIdPasien.IsEnabled = true;
                    TxtIdPasien.Focus();
                }
            }
        }

        public ReportDataModel GetPatientData()
        {
            // Validasi sadar: Jika suster tidak memilih gender, kembalikan teks kosong, bukan instruksi prompt UI
            string selectedGender = CmbGender.SelectedIndex == 0 ? string.Empty : (CmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            return new ReportDataModel
            {
                IdPasien = TxtIdPasien.Text?.Trim() ?? string.Empty,
                Nama = TxtNama.Text?.Trim() ?? string.Empty,
                Hospital = TxtRs.Text?.Trim() ?? string.Empty,
                Address = TxtAlamatRs.Text?.Trim() ?? string.Empty,
                TanggalLahir = DpTanggalLahir.SelectedDate ?? DateTime.MinValue,
                Gender = selectedGender,
                Dokter = CmbDokter.SelectedItem?.ToString() ?? string.Empty,
                Keluhan = TxtKeluhan.Text?.Trim() ?? string.Empty,
                Diagnosis = TxtDiagnosis.Text?.Trim() ?? string.Empty,
                ObatPremedikasi = TxtObatPremedikasi.Text?.Trim() ?? string.Empty,
                Alat = TxtAlat.Text?.Trim() ?? string.Empty
            };
        }

        public void ResetFormPasien()
        {
            TxtIdPasien.Clear();
            TxtNama.Clear();
            DpTanggalLahir.SelectedDate = null;
            CmbGender.SelectedIndex = 0;
            CmbDokter.SelectedIndex = 0;
            TxtKeluhan.Clear();
            TxtDiagnosis.Clear();
            TxtObatPremedikasi.Clear();
            TxtAlat.Clear();
        }
    }
}