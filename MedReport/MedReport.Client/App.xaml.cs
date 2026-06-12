using MedReport.Client.Services;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MedReport.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Muat konfigurasi API saat aplikasi pertama kali menyala
            ConfigService.LoadConfig();

            // Pengaturan lisensi QuestPDF bawaan (Sangat Penting)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // -------------------------------------------------------------------------
        // GARDA PUNGKAS: Menangkap Semua Eror Gaib yang Lolos dari Try-Catch
        // -------------------------------------------------------------------------
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 1. AMANKAN JEJAK EROR (AUDIT TRAIL LOGGING)
            string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalApp_Api", "Logs");
            string logFilePath = Path.Combine(logFolderPath, $"CrashLog_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                // Catat kronologi eror super detail untuk bahan investigasi developer
                string logMessage = $"==================================================\n" +
                                    $"WAKTU KEJADIAN : {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"PESAN EROR     : {e.Exception.Message}\n" +
                                    $"SUMBER         : {e.Exception.Source}\n" +
                                    $"STACK TRACE    : \n{e.Exception.StackTrace}\n" +
                                    $"==================================================\n\n";

                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // Jika gagal menulis log (misal harddisk penuh), ignore agar tidak terjadi infinite loop crash
            }

            // 2. TAMPILKAN PESAN EDUKATIF DAN PENYELAMATAN DATA (GRACEFUL DEGRADATION)
            string pesanMedis = "Sistem MedReport mengalami gangguan internal yang tidak terduga.\n\n" +
                                "TINDAKAN AMAN UNTUK PERAWAT / DOKTER:\n" +
                                "1. Jangan khawatir, aplikasi telah mencegah 'Force Close' agar data di layar tidak hilang.\n" +
                                "2. Harap CATAT/FOTO data pasien yang ada di layar saat ini agar tidak hilang.\n" +
                                "3. Sangat disarankan untuk MENUTUP APLIKASI dan MEMBUKANYA KEMBALI sebelum membuat laporan baru demi menjaga kebersihan memori rekam medis.\n\n" +
                                $"Detail Kegagalan: {e.Exception.Message}";

            MessageBox.Show(pesanMedis, "Pemberitahuan Keamanan Sistem", MessageBoxButton.OK, MessageBoxImage.Error);

            // 3. BLOKIR FORCE CLOSE: Biarkan aplikasi tetap hidup agar suster bisa mencatat/menyalin data yang ada di form
            e.Handled = true;
        }
    }
}