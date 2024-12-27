using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class EditPatientPage : ContentPage
    {
        public Patient SelectedPatient { get; set; }
        private readonly DatabaseService _databaseService;

        public EditPatientPage(int patientId, DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            SelectedPatient = new Patient();
            BindingContext = this;

            LoadPatient(patientId);
        }

        private async void LoadPatient(int patientId)
        {
            try
            {
                var patient = await _databaseService.GetPatientByIdAsync(patientId);
                if (patient != null)
                {
                    SelectedPatient = patient;
                    OnPropertyChanged(nameof(SelectedPatient));
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ danych pacjenta.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading patient: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                await _databaseService.SavePatientAsync(SelectedPatient);
                await DisplayAlert("Sukces", "Dane pacjenta zosta³y zapisane.", "OK");
                await Navigation.PopAsync();
            }
            catch (InvalidOperationException ex)
            {
                await DisplayAlert("B³¹d", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving patient: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ danych pacjenta.", "OK");
            }
        }


        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
