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
using MedReport.Client.Utilities;

namespace MedReport.Client.Views
{
    public class MedicalImageUiModel
    {
        public MedicalImageModel DataModel { get; set; } = new MedicalImageModel();
        public BitmapImage? Thumbnail { get; set; }
    }

    public partial class ImageGridView : UserControl
    {
        public ObservableCollection<MedicalImageUiModel> SelectedPhotos { get; set; }

        public ImageGridView()
        {
            InitializeComponent();
            SelectedPhotos = new ObservableCollection<MedicalImageUiModel>();
            PhotoGallery.ItemsSource = SelectedPhotos;
            PerbaruiIndikatorKuota(); // Inisialisasi tampilan awal
        }

        public List<MedicalImageModel> SelectedPhotosDataModels
        {
            get { return SelectedPhotos.Select(p => p.DataModel).ToList(); }
        }

        // SOLUSI AUDIT MUTU 1: Satukan logika pembaruan teks kuota ke dalam satu fungsi terpusat
        private void PerbaruiIndikatorKuota()
        {
            TxtPhotoCount.Text = $" ({SelectedPhotos.Count}/8 Photos)";
        }

        public void ResetGaleri()
        {
            SelectedPhotos.Clear();
            PerbaruiIndikatorKuota();
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

                    // SOLUSI AUDIT CRITICAL 2: Defensive check menggunakan string.Equals secara aman dari bahaya Null
                    if (SelectedPhotos.Any(p => p.DataModel?.OriginalPath != null &&
                        string.Equals(p.DataModel.OriginalPath, fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    try
                    {
                        BitmapImage thumbnailBitmap = ImageHelper.LoadThumbnailWithoutLocking(fileName);

                        if (thumbnailBitmap != null)
                        {
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

                PerbaruiIndikatorKuota();
            }
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            MedicalImageUiModel imageToRemove = btn.Tag as MedicalImageUiModel;

            if (imageToRemove != null)
            {
                SelectedPhotos.Remove(imageToRemove);
                PerbaruiIndikatorKuota();
            }
        }
    }
}