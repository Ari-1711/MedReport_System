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
using MedReport.Client.Utilities; // WAJIB: Panggil folder Utilities tempat ImageHelper berada

namespace MedReport.Client.Views
{
    /// <summary>
    /// MODEL KHUSUS UI (UI-Wrapper Model)
    /// Digunakan untuk menjembatani MedicalImageModel murni dengan kebutuhan render elemen XAML.
    /// Menjaga berkas di folder 'Models' tetap bersih dari dependensi WPF.
    /// </summary>
    public class MedicalImageUiModel
    {
        public MedicalImageModel DataModel { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }

    public partial class ImageGridView : UserControl
    {
        // Koleksi UI sekarang mengikat MedicalImageUiModel agar XAML bisa membaca properti .Thumbnail
        public ObservableCollection<MedicalImageUiModel> SelectedPhotos { get; set; }

        public ImageGridView()
        {
            InitializeComponent();
            SelectedPhotos = new ObservableCollection<MedicalImageUiModel>();
            PhotoGallery.ItemsSource = SelectedPhotos;
        }

        // Properti publik untuk MainWindow agar tetap bisa mengambil list data model murninya saat cetak PDF
        public List<MedicalImageModel> SelectedPhotosDataModels
        {
            get { return SelectedPhotos.Select(p => p.DataModel).ToList(); }
        }

        public void ResetGaleri()
        {
            SelectedPhotos.Clear();
            TxtPhotoCount.Text = " (0/8 Photos)";
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPhotos.Count >= 8)
            {
                MessageBox.Show("Maksimal 8 foto saja.", "Batas Maksimal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    if (SelectedPhotos.Count >= 8)
                    {
                        MessageBox.Show("Beberapa foto tidak ditambahkan karena melebihi batas maksimal (8 foto).", "Batas Maksimal", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }

                    // Cek duplikasi berdasarkan data model di dalam wrapper
                    if (SelectedPhotos.Any(p => p.DataModel.OriginalPath.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    try
                    {
                        // IMPLEMENTASI OPTIMASI: Panggil fungsi dari Utilities tanpa mengunci file fisik
                        BitmapImage thumbnailBitmap = ImageHelper.LoadThumbnailWithoutLocking(fileName);

                        if (thumbnailBitmap != null)
                        {
                            // Bungkus model data murni dan properti visual ke dalam wrapper UI
                            SelectedPhotos.Add(new MedicalImageUiModel
                            {
                                DataModel = new MedicalImageModel { OriginalPath = fileName },
                                Thumbnail = thumbnailBitmap
                            });
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"File '{System.IO.Path.GetFileName(fileName)}' rusak atau tidak dapat dibaca oleh sistem.", "Error Import", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                TxtPhotoCount.Text = $" ({SelectedPhotos.Count}/8 Photos)";
            }
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            // Unboxing ke UI model wrapper
            MedicalImageUiModel imageToRemove = btn.Tag as MedicalImageUiModel;

            if (imageToRemove != null)
            {
                SelectedPhotos.Remove(imageToRemove);
                TxtPhotoCount.Text = $" ({SelectedPhotos.Count}/8 Photos)";
            }
        }
    }
}