using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Linq;
using MedReport.Client.Models;

namespace MedReport.Client.Views
{
    public partial class ImageGridView : UserControl
    {
        // -------------------------------------------------------------------------
        // OBSERVABLE COLLECTION: Jantung dari Data Binding UI
        // Menggunakan ObservableCollection agar setiap kali ada foto ditambah/dihapus, 
        // tampilan galeri di layar (PhotoGallery) otomatis memperbarui dirinya sendiri
        // tanpa perlu kita tulis kode manual untuk me-refresh layar.
        // -------------------------------------------------------------------------
        public ObservableCollection<MedicalImageModel> SelectedPhotos { get; set; }

        // -------------------------------------------------------------------------
        // CONSTRUCTOR: Dipanggil saat UserControl Galeri Foto dimuat
        // -------------------------------------------------------------------------
        public ImageGridView()
        {
            InitializeComponent();
            SelectedPhotos = new ObservableCollection<MedicalImageModel>();

            // Menyambungkan "Pipa Data" dari variabel SelectedPhotos ke elemen UI XAML (ItemsControl)
            PhotoGallery.ItemsSource = SelectedPhotos;
        }

        // -------------------------------------------------------------------------
        // FUNGSI RESET: Membersihkan galeri dan indikator jumlah foto
        // Dipanggil dari MainWindow setelah laporan PDF sukses dibuat agar foto
        // pasien sebelumnya tidak tercampur ke laporan pasien berikutnya.
        // -------------------------------------------------------------------------
        public void ResetGaleri()
        {
            SelectedPhotos.Clear();
            TxtPhotoCount.Text = " (0/8 Photos)";
        }

        // -------------------------------------------------------------------------
        // FUNGSI UTAMA: Menambahkan gambar ke dalam galeri dengan proteksi memori ekstrem
        // -------------------------------------------------------------------------
        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            // Proteksi Lapis 1: Mencegah masuk jika dari awal galeri sudah penuh (8 foto)
            if (SelectedPhotos.Count >= 8)
            {
                MessageBox.Show("Maksimal 8 foto saja.", "Batas Maksimal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            openFileDialog.Multiselect = true; // Mengizinkan perawat memilih banyak foto sekaligus (Blok file)

            if (openFileDialog.ShowDialog() == true)
            {
                // Looping untuk memproses setiap file yang dipilih oleh perawat
                foreach (string fileName in openFileDialog.FileNames)
                {
                    // Proteksi Lapis 2: Jika perawat memilih 10 foto sekaligus, 
                    // sistem akan memotong pemrosesan tepat setelah foto ke-8.
                    if (SelectedPhotos.Count >= 8)
                    {
                        MessageBox.Show("Beberapa foto tidak ditambahkan karena melebihi batas maksimal (8 foto).", "Batas Maksimal", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break; // Hentikan perulangan secara paksa
                    }

                    // Proteksi Lapis 3: CEK DUPLIKASI EKSTREM
                    // Mengecek apakah jalur file (path) yang sama persis sudah ada di dalam koleksi.
                    // Mencegah perawat tidak sengaja memasukkan 1 gambar yang sama dua kali.
                    if (SelectedPhotos.Any(p => p.OriginalPath.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue; // Lewati (skip) gambar ini, lanjut proses file berikutnya
                    }

                    // Proteksi Lapis 4: PERLINDUNGAN FILE KORUP & MANAJEMEN RAM
                    try
                    {
                        BitmapImage thumbnailBitmap = new BitmapImage();

                        // Proses perakitan objek gambar secara manual untuk mengunci opsi performa
                        thumbnailBitmap.BeginInit();
                        thumbnailBitmap.UriSource = new Uri(fileName);

                        // DECODE PIXEL: Mencegah aplikasi meload gambar endoskopi 4K (yang memakan RAM bergiga-giga).
                        // Kita paksa render maksimal lebar 300px saja untuk keperluan *preview* di layar.
                        thumbnailBitmap.DecodePixelWidth = 300;

                        // CACHE OPTION: Memaksa file langsung di-copy ke RAM lalu dilepas dari harddisk,
                        // sehingga file asli tidak berstatus "In Use/Terkunci" jika user ingin menghapusnya.
                        thumbnailBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        thumbnailBitmap.EndInit();

                        // FREEZE: Membekukan gambar UI agar tidak bisa dimodifikasi lagi. 
                        // Ini membuat WPF merender gambar secara jauh lebih cepat dan ringan di memori.
                        if (thumbnailBitmap.CanFreeze)
                        {
                            thumbnailBitmap.Freeze();
                        }

                        // Memasukkan hasil render ke dalam Data Model yang akan dibaca oleh pembuat PDF
                        SelectedPhotos.Add(new MedicalImageModel
                        {
                            OriginalPath = fileName, // Jalur file asli untuk ditarik PDF Engine nanti
                            Thumbnail = thumbnailBitmap // Gambar resolusi rendah untuk dipajang di layar UI
                        });
                    }
                    catch (Exception)
                    {
                        // Jika 1 file korup, jangan biarkan aplikasi force close.
                        // Beritahu user file mana yang rusak, lalu lanjutkan loop ke file berikutnya.
                        MessageBox.Show($"File '{System.IO.Path.GetFileName(fileName)}' rusak atau tidak dapat dibaca oleh sistem.", "Error Import", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Perbarui teks indikator jumlah (misal: "3/8 Photos")
                TxtPhotoCount.Text = $" ({SelectedPhotos.Count}/8 Photos)";
            }
        }

        // -------------------------------------------------------------------------
        // FUNGSI PENGHAPUSAN: Menghapus 1 gambar spesifik dari galeri
        // -------------------------------------------------------------------------
        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            // 1. Identifikasi tombol mana yang sedang diklik
            Button btn = (Button)sender;

            // 2. Ekstrak data model spesifik dari tombol tersebut (di XAML, kita mengikat Tag ke {Binding})
            MedicalImageModel imageToRemove = btn.Tag as MedicalImageModel;

            // 3. Jika datanya valid, hapus dari ObservableCollection
            // (Begitu dihapus, gambar akan otomatis menghilang dari layar berkat fitur Data Binding)
            if (imageToRemove != null)
            {
                SelectedPhotos.Remove(imageToRemove);
                TxtPhotoCount.Text = $" ({SelectedPhotos.Count}/8 Photos)"; // Update teks indikator
            }
        }
    }
}