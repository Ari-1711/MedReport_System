using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MedReport.Client.Models;
using MedReport.Client.ViewModels;

namespace MedReport.Client.Views
{
    public partial class PatientFormView : UserControl
    {
        public PatientFormView()
        {
            InitializeComponent();
            // Pasang jembatan pintar ke DataContext UI
            this.DataContext = new PatientFormViewModel();
        }

        private void NameValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^a-zA-Z\s\.\,\']");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NameTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex(@"[^a-zA-Z\s\.\,\']");
                if (regex.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void IdTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex(@"[^a-zA-Z0-9\-]");
                if (regex.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private async void TxtIdPasien_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (this.DataContext is PatientFormViewModel viewModel && !viewModel.IsBusy)
                {
                    TxtIdPasien.IsEnabled = false;
                    await viewModel.CariPasienExecuteAsync();
                    TxtIdPasien.IsEnabled = true;
                    TxtIdPasien.Focus();
                }
            }
        }

        // Bridge method agar MainWindow lama lu tidak error saat memanggil fungsi penarik data
        public ReportDataModel GetPatientData()
        {
            if (this.DataContext is PatientFormViewModel viewModel)
            {
                return viewModel.GetPatientReportData();
            }
            return new ReportDataModel();
        }

        public void ResetFormPasien()
        {
            if (this.DataContext is PatientFormViewModel viewModel)
            {
                viewModel.ResetFormPasien();
            }
        }
    }
}