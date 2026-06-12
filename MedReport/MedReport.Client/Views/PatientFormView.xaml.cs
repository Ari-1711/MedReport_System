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
        // =========================================================================
        // IMPLEMENTASI OPTIMASI 2: INSTANCE TETAP & MEKANISME GEMBOK
        // =========================================================================
        private readonly HospitalApiService _apiService = new HospitalApiService();
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

                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string response = await client.GetStringAsync(apiUrlDokter);
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
                // Safety net agar UI tidak freeze jika API down
            }
        }

        private void LetterValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void LetterTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[0-9]+");

                if (regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // -------------------------------------------------------------------------
        // EVENT LISTENER: Sinkronisasi Data Model Pasien Baru
        // -------------------------------------------------------------------------
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

                        // PERBAIKAN 1: Kembalikan DateTime murni ke DateTime? agar sinkron dengan DatePicker WPF
                        if (hasil.TanggalLahir == DateTime.MinValue)
                            DpTanggalLahir.SelectedDate = null;
                        else
                            DpTanggalLahir.SelectedDate = hasil.TanggalLahir;

                        // PERBAIKAN 2: Gunakan properti .RawGender untuk pencocokan elemen ComboBox UI
                        if (!string.IsNullOrEmpty(hasil.RawGender))
                        {
                            // Gunakan hasil normalisasi data internal untuk akurasi pencarian di UI
                            string targetGenderText = hasil.NormalizedGender == GenderType.LakiLaki ? "Laki-laki" : "Perempuan";

                            foreach (ComboBoxItem item in CmbGender.Items)
                            {
                                string itemText = item.Content.ToString();
                                // Cek kecocokan teks murni UI ("Laki-laki"/"Perempuan") atau kode mentah API ("L"/"P")
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
            return new ReportDataModel
            {
                IdPasien = TxtIdPasien.Text?.Trim(),
                Nama = TxtNama.Text?.Trim(),
                Hospital = TxtRs.Text?.Trim(),
                Address = TxtAlamatRs.Text?.Trim(),

                TanggalLahir = DpTanggalLahir.SelectedDate.HasValue
                               ? DpTanggalLahir.SelectedDate.Value.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID"))
                               : "",

                Gender = (CmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                Dokter = CmbDokter.SelectedItem?.ToString(),

                Keluhan = TxtKeluhan.Text?.Trim(),
                Diagnosis = TxtDiagnosis.Text?.Trim(),
                ObatPremedikasi = TxtObatPremedikasi.Text?.Trim(),
                Alat = TxtAlat.Text?.Trim()
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