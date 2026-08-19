using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MedReport.Client.Models;
using MedReport.Client.Services;

namespace MedReport.Client.ViewModels
{
    public class PatientFormViewModel : BaseViewModel
    {
        private readonly HospitalApiService _apiService;
        private static readonly HttpClient _sharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        private PatientApiModel? _patientData;
        private string _searchIdInput = string.Empty;
        private bool _isBusy;

        private string _hospitalName = ConfigService.HospitalName;
        private string _hospitalAddress = ConfigService.HospitalAddress;
        private string _keluhan = string.Empty;
        private string _diagnosis = string.Empty;
        private string _obatPremedikasi = string.Empty;
        private string _alat = string.Empty;
        private string _selectedDokter = string.Empty;
        private int _selectedGenderIndex = 0;

        // Properties
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public string SearchIdInput { get => _searchIdInput; set => SetProperty(ref _searchIdInput, value); }
        public PatientApiModel? PatientData { get => _patientData; set => SetProperty(ref _patientData, value); }

        public string HospitalName
        {
            get => _hospitalName;
            set => SetProperty(ref _hospitalName, value);
        }

        public string HospitalAddress
        {
            get => _hospitalAddress;
            set => SetProperty(ref _hospitalAddress, value);
        }

        // Sekarang method RefreshHospitalData lu di bawah ini dijamin tidak akan eror lagi:
        public void RefreshHospitalData()
        {
            HospitalName = ConfigService.HospitalName;
            HospitalAddress = ConfigService.HospitalAddress;
        }

        public string Keluhan { get => _keluhan; set => SetProperty(ref _keluhan, value); }
        public string Diagnosis { get => _diagnosis; set => SetProperty(ref _diagnosis, value); }
        public string ObatPremedikasi { get => _obatPremedikasi; set => SetProperty(ref _obatPremedikasi, value); }
        public string Alat { get => _alat; set => SetProperty(ref _alat, value); }
        public string SelectedDokter { get => _selectedDokter; set => SetProperty(ref _selectedDokter, value); }
        public int SelectedGenderIndex { get => _selectedGenderIndex; set => SetProperty(ref _selectedGenderIndex, value); }

        // Koleksi Daftar Dokter untuk di-binding ke ComboBox UI
        public ObservableCollection<string> DaftarDokter { get; set; } = new ObservableCollection<string>();

        public PatientFormViewModel()
        {
            _apiService = new HospitalApiService();
            PatientData = new PatientApiModel();

            // Pemicu otomatis load dokter saat init (Non-blocking)
            _ = MuatDaftarDokterAsync();
        }

        public async Task MuatDaftarDokterAsync()
        {
            try
            {
                string apiUrlDokter = ConfigService.GetValue("DoctorApiUrl");
                if (string.IsNullOrWhiteSpace(apiUrlDokter)) apiUrlDokter = "http://localhost:3000/dokter";

                string keyNamaDokter = ConfigService.GetMappingValue("DoctorNameKey");
                if (string.IsNullOrWhiteSpace(keyNamaDokter)) keyNamaDokter = "nama";

                string response = await _sharedClient.GetStringAsync(apiUrlDokter);
                var dokterList = JsonNode.Parse(response)?.AsArray();

                DaftarDokter.Clear();
                if (dokterList != null)
                {
                    foreach (var dok in dokterList)
                    {
                        var nama = dok?[keyNamaDokter]?.ToString();
                        if (!string.IsNullOrEmpty(nama)) DaftarDokter.Add(nama);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERR_LOAD_DOKTER]: {ex.Message}");
            }
        }

        public async Task CariPasienExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchIdInput) || IsBusy) return;

            IsBusy = true;
            try
            {
                var hasil = await _apiService.CariPasienAsync(SearchIdInput.Trim());
                if (hasil != null)
                {
                    PatientData = hasil;

                    // Set gender combobox secara otomatis dari hasil normalisasi model
                    SelectedGenderIndex = hasil.NormalizedGender == GenderType.LakiLaki ? 1 :
                                          hasil.NormalizedGender == GenderType.Perempuan ? 2 : 0;
                }
                else
                {
                    System.Windows.MessageBox.Show("[ERR_PATIENT_VM_02]: Pasien tidak ditemukan.", "Peringatan", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    ResetFormPasien();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"[ERR_PATIENT_VM_01]: Terjadi gangguan koneksi: {ex.Message}", "Gangguan Jaringan", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public ReportDataModel GetPatientReportData()
        {
            string genderText = SelectedGenderIndex == 1 ? "Laki-laki" :
                                SelectedGenderIndex == 2 ? "Perempuan" : string.Empty;

            return new ReportDataModel
            {
                IdPasien = SearchIdInput?.Trim() ?? string.Empty,
                Nama = PatientData?.Nama?.Trim() ?? string.Empty,
                Hospital = HospitalName?.Trim() ?? string.Empty,
                Address = HospitalAddress?.Trim() ?? string.Empty,
                TanggalLahir = PatientData?.TanggalLahir ?? DateTime.MinValue,
                Gender = genderText,
                Dokter = SelectedDokter ?? string.Empty,
                Keluhan = Keluhan?.Trim() ?? string.Empty,
                Diagnosis = Diagnosis?.Trim() ?? string.Empty,
                ObatPremedikasi = ObatPremedikasi?.Trim() ?? string.Empty,
                Alat = Alat?.Trim() ?? string.Empty
            };
        }

        public void ResetFormPasien()
        {
            SearchIdInput = string.Empty;
            PatientData = new PatientApiModel();
            SelectedGenderIndex = 0;
            Keluhan = string.Empty;
            Diagnosis = string.Empty;
            ObatPremedikasi = string.Empty;
            Alat = string.Empty;
        }
    }
}