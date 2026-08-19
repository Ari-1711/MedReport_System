using System;
using System.Security;
using System.Windows;
using MedReport.Client.Services;

namespace MedReport.Client.Views
{
    public partial class PinValidationWindow : Window
    {
        public bool IsAuthenticated { get; private set; } = false;

        public PinValidationWindow()
        {
            InitializeComponent();
            PbPin.Focus();
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            // Ambil SecureString langsung dari password box
            SecureString securePin = PbPin.SecurePassword;

            if (securePin == null || securePin.Length == 0)
            {
                ShowAccessDenied();
                return;
            }

            // Validasi PIN lewat kelas statis (Aman dari memory dumping)
            if (ConfigService.ValidateItPin(securePin))
            {
                IsAuthenticated = true;
                DialogResult = true;
                Close();
            }
            else
            {
                ShowAccessDenied();
            }
        }

        private void ShowAccessDenied()
        {
            MessageBox.Show(
                "PIN Otorisasi Salah! Akses modifikasi konfigurasi jaringan ditolak.",
                "Akses Ditolak",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);

            PbPin.Clear();
            PbPin.Focus();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Catatan: Method PbPin_KeyDown telah dihapus karena fungsinya sudah diambil alih 
        // secara native dan efisien oleh properti IsDefault="True" di file .xaml lu.
    }
}