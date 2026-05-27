using System;
using System.Collections.Generic;
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
using System.Text.RegularExpressions;
using MedReport.Client.Models;
using System.Net.Http;

namespace MedReport.Client.Views
{
    public partial class PatientFormView : UserControl
    {
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
            // Menggunakan jalur absolut agar tidak error saat aplikasi dijalankan dari Shortcut Desktop
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string jsonString = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.Nodes.JsonNode.Parse(jsonString);

                    TxtRs.Text = config?["HospitalName"]?.ToString() ?? "";
                    TxtAlamatRs.Text = config?["HospitalAddress"]?.ToString() ?? "";
                }
                catch
                {
                    // Sengaja dibiarkan kosong (silent ignore). Jika config rusak, 
                    // teks akan tetap kosong namun tidak membuat aplikasi crash.
                }
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI INIT: Menarik daftar dokter dari server API
        // Memiliki sabuk pengaman agar UI tidak terkunci jika server mati
        // -------------------------------------------------------------------------
        private async void MuatDaftarDokter()
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (!System.IO.File.Exists(configPath))
                {
                    // Peringatan visual jika teknisi lapangan lupa menyertakan file config
                    CmbDokter.Items.Add("Error: config.json tidak ditemukan");
                    CmbDokter.SelectedIndex = 0;
                    return;
                }

                string jsonString = System.IO.File.ReadAllText(configPath);
                var config = System.Text.Json.Nodes.JsonNode.Parse(jsonString);

                // Menarik URL dan Key respons API dari config
                string apiUrlDokter = config?["DoctorApiUrl"]?.ToString();
                string keyNamaDokter = config?["Mapping"]?["DoctorNameKey"]?.ToString();

                // Validasi: Cegah aplikasi mencari alamat kosong (mencegah crash HttpClient)
                if (string.IsNullOrWhiteSpace(apiUrlDokter) || string.IsNullOrWhiteSpace(keyNamaDokter))
                {
                    CmbDokter.Items.Add("Error: API Dokter di config kosong!");
                    CmbDokter.SelectedIndex = 0;
                    return;
                }

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Batas waktu maksimal antre jaringan (5 detik)

                string response = await client.GetStringAsync(apiUrlDokter);
                var dokterList = System.Text.Json.Nodes.JsonNode.Parse(response)?.AsArray();

                CmbDokter.Items.Clear();

                if (dokterList != null)
                {
                    // Masukkan satu per satu nama dokter ke dalam ComboBox (Dropdown UI)
                    foreach (var dok in dokterList)
                    {
                        CmbDokter.Items.Add(dok[keyNamaDokter]?.ToString());
                    }
                    // Pilih urutan pertama secara default jika data tersedia
                    if (CmbDokter.Items.Count > 0) CmbDokter.SelectedIndex = 0;
                }
            }
            catch (Exception)
            {
                // Tangkapan layar jika API server dokter terputus/mati
                CmbDokter.Items.Add("Gagal koneksi ke server Dokter");
                CmbDokter.SelectedIndex = 0;
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI VALIDASI UI: Mencegah pengguna mengetik angka (0-9) di keyboard
        // Dipakai untuk kolom teks yang murni hanya boleh berisi huruf (seperti Nama)
        // -------------------------------------------------------------------------
        private void LetterValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[0-9]+");
            // e.Handled = true berarti input ditolak. Jika yang diketik adalah angka, tolak.
            e.Handled = regex.IsMatch(e.Text);
        }

        // -------------------------------------------------------------------------
        // FUNGSI VALIDASI UI: Mencegah pengguna melakukan Copy-Paste (Ctrl+V) yang memuat angka
        // Menutup celah bypass dari fungsi LetterValidationTextBox
        // -------------------------------------------------------------------------
        private void LetterTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[0-9]+");

                if (regex.IsMatch(text))
                {
                    e.CancelCommand(); // Batalkan aksi paste jika terdeteksi angka
                }
            }
            else
            {
                e.CancelCommand(); // Batalkan jika yang di-paste bukan tipe teks (misal: gambar)
            }
        }

        // -------------------------------------------------------------------------
        // EVENT LISTENER: Berjalan saat pengguna menekan tombol pada keyboard di kolom ID Pasien
        // -------------------------------------------------------------------------
        private async void TxtIdPasien_KeyDown(object sender, KeyEventArgs e)
        {
            // Logika hanya berjalan ketika suster menekan tombol 'Enter'
            if (e.Key == Key.Enter)
            {
                string idInput = TxtIdPasien.Text.Trim(); // .Trim() membersihkan spasi kiri-kanan
                if (string.IsNullOrEmpty(idInput)) return;

                // Memanggil Service jaringan (Sang Kurir) untuk mencari data ke API
                var apiService = new MedReport.Client.Services.HospitalApiService();
                var hasil = await apiService.CariPasienAsync(idInput);

                if (hasil != null)
                {
                    // Pasien ditemukan, isi otomatis form UI
                    TxtNama.Text = hasil.Nama;

                    // Oper tipe data Type-Safe (DateTime?) secara langsung ke DatePicker UI
                    DpTanggalLahir.SelectedDate = hasil.TanggalLahir;

                    // Logika pencarian nilai ComboBox untuk Jenis Kelamin
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
                    // Jika server membalas 404 (Pasien Tidak Ditemukan)
                    MessageBox.Show("Pasien tidak ditemukan.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Bersihkan sisa data sebelumnya agar tidak salah input
                    TxtNama.Clear();
                    DpTanggalLahir.SelectedDate = null;
                }
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI PENGUMPUL DATA: Dipanggil oleh MainWindow sebelum membuat PDF
        // Bertugas membungkus semua inputan layar menjadi satu objek Type-Safe (ReportDataModel)
        // -------------------------------------------------------------------------
        public ReportDataModel GetPatientData()
        {
            return new ReportDataModel
            {
                // 1. Mengisi objek Patient (Komposisi)
                Patient = new PatientApiModel
                {
                    IdPasien = TxtIdPasien.Text?.Trim(),
                    Nama = TxtNama.Text?.Trim(),


                    TanggalLahir = DpTanggalLahir.SelectedDate,

                    // Ekstraksi nilai teks dari elemen dropdown (ComboBox)
                    Gender = (CmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString()
                },

                Hospital = TxtRs.Text?.Trim(),
                Address = TxtAlamatRs.Text?.Trim(),
                Dokter = CmbDokter.SelectedItem?.ToString(), // Langsung baca string karena item dari API

                // Input catatan klinis panjang
                Keluhan = TxtKeluhan.Text?.Trim(),
                Diagnosis = TxtDiagnosis.Text?.Trim(),
                ObatPremedikasi = TxtObatPremedikasi.Text?.Trim(),
                Alat = TxtAlat.Text?.Trim()
            };
        }

        // -------------------------------------------------------------------------
        // FUNGSI RESET: Membersihkan area layar klinis (Mencegah kontaminasi data pasien)
        // Dipanggil oleh MainWindow secara otomatis HANYA SETELAH laporan PDF berhasil dicetak
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
            // Catatan: TxtRs dan TxtAlamatRs sengaja tidak di-clear karena itu identitas tetap RS
        }
    }
}