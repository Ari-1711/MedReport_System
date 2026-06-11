using MedReport.Client.Models;
using MedReport.Client.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
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

namespace MedReport.Client.Views
{
    public partial class PatientFormView : UserControl
    {
        // =========================================================================
        // IMPLEMENTASI OPTIMASI 2: INSTANCE TETAP & MEKANISME GEMBOK
        // =========================================================================
        // 1. Instansiasi sekali saja di tingkat kelas agar hemat RAM (Singleton-like)
        private readonly HospitalApiService _apiService = new HospitalApiService();

        // 2. Variabel gembok (Flag) untuk mencegah suster melakukan spam Enter
        private bool _isSearching = false;

        // -------------------------------------------------------------------------
        // CONSTRUCTOR: Dipanggil pertama kali saat tampilan (View) ini dimuat
        // -------------------------------------------------------------------------
        public PatientFormView()
        {
            InitializeComponent();
            MuatTemplateRs();     // Tarik data rumah sakit ke layar
            MuatDaftarDokter();   // Tarik daftar nama dokter dari API
        }

        // -------------------------------------------------------------------------
        // FUNGSI INIT: Membaca nama & alamat RS dari config.json
        // Tujuannya agar aplikasi bisa dipakai di RS mana saja tanpa ubah kode (Hardcode)
        // -------------------------------------------------------------------------
        private void MuatTemplateRs()
        {
            TxtRs.Text = ConfigService.HospitalName;
            TxtAlamatRs.Text = ConfigService.HospitalAddress;
        }

        // -------------------------------------------------------------------------
        // FUNGSI INIT: Menarik daftar dokter dari server API
        // Memiliki sabuk pengaman agar UI tidak terkunci jika server mati
        // -------------------------------------------------------------------------
        private async void MuatDaftarDokter()
        {
            try
            {
                // Ambil URL dokter
                string apiUrlDokter = ConfigService.GetValue("DoctorApiUrl");

                // SOLUSI: Jika config belum siap/kosong, gunakan URL cadangan (localhost)
                if (string.IsNullOrWhiteSpace(apiUrlDokter))
                {
                    apiUrlDokter = "http://localhost:3000/dokter";
                }

                string keyNamaDokter = ConfigService.GetMappingValue("DoctorNameKey");
                // Jika mapping key kosong, gunakan default "nama"
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
                // Jangan biarkan aplikasi crash jika server API dokter mati
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI VALIDASI UI: Mencegah pengguna mengetik angka (0-9) di keyboard
        // -------------------------------------------------------------------------
        private void LetterValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // -------------------------------------------------------------------------
        // FUNGSI VALIDASI UI: Mencegah pengguna melakukan Copy-Paste (Ctrl+V) memuat angka
        // -------------------------------------------------------------------------
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
        // EVENT LISTENER: Dioptimalkan dengan Async Lock & Visual Feedback
        // -------------------------------------------------------------------------
        private async void TxtIdPasien_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // JIKA GEMBOK AKTIF: Langsung batalkan proses (abaikan spam Enter)
                if (_isSearching) return;

                string idInput = TxtIdPasien.Text.Trim();
                if (string.IsNullOrEmpty(idInput)) return;

                try
                {
                    // PASANG GEMBOK & MATIKAN INPUT (Visual Feedback agar suster tahu aplikasi sedang bekerja)
                    _isSearching = true;
                    TxtIdPasien.IsEnabled = false;

                    // Gunakan _apiService tingkat kelas yang hemat memori
                    var hasil = await _apiService.CariPasienAsync(idInput);

                    if (hasil != null)
                    {
                        TxtNama.Text = hasil.Nama;
                        DpTanggalLahir.SelectedDate = hasil.TanggalLahir;

                        if (!string.IsNullOrEmpty(hasil.Gender))
                        {
                            foreach (ComboBoxItem item in CmbGender.Items)
                            {
                                if (item.Content.ToString() == hasil.Gender)
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
                    // LEPAS GEMBOK & BUKA KEMBALI INPUT (Selalu dieksekusi baik berhasil maupun error)
                    _isSearching = false;
                    TxtIdPasien.IsEnabled = true;
                    TxtIdPasien.Focus(); // Kembalikan fokus kursor agar suster nyaman
                }
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI PENGUMPUL DATA: Dipanggil oleh MainWindow sebelum membuat PDF
        // -------------------------------------------------------------------------
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

        // -------------------------------------------------------------------------
        // FUNGSI RESET: Membersihkan area layar klinis
        // -------------------------------------------------------------------------
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