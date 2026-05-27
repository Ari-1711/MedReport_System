using QuestPDF.Infrastructure;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace MedReport.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Pengaturan lisensi QuestPDF bawaan Anda (Sangat Penting)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // --- TAMBAHKAN TANGKAPAN ERROR GLOBAL DI SINI ---
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Tampilkan pesan peringatan profesional kepada perawat/dokter
            MessageBox.Show(
                $"Terjadi gangguan pada sistem. Laporan belum dapat diproses.\n\nDetail teknis: {e.Exception.Message}",
                "Peringatan Sistem",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Mencegah aplikasi Force Close (Mati mendadak)
            e.Handled = true;
        }
    }
}
