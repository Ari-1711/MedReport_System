using System.Windows;
using System.Windows.Input;

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
            // Proteksi PIN Statis Khusus Tim IT Rumah Sakit
            const string PinItRs = "2026";

            if (PbPin.Password == PinItRs)
            {
                IsAuthenticated = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "PIN Otorisasi Salah! Akses modifikasi konfigurasi jaringan ditolak.",
                    "Akses Ditolak",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                PbPin.Clear();
                PbPin.Focus();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PbPin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnVerify_Click(this, new RoutedEventArgs());
            }
        }
    }
}