using HospitalManagementData;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class EditPatientPage : ContentPage
    {
        public List<BloodType> BloodTypes { get; set; }
        public Patient SelectedPatient { get; set; }
        public List<int> AvailableBeds { get; set; } = new List<int>();
        private readonly DatabaseService _databaseService;
        private readonly BedService _bedService;
        private readonly BloodService _bloodService;

        public EditPatientPage(int patientId, DatabaseService databaseService, BedService bedService, BloodService bloodService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _bedService = bedService; // Inicjalizacja BedService
            _bloodService = bloodService;

            SelectedPatient = new Patient();
            AvailableBeds = new List<int>();
            BindingContext = this;

            LoadPatient(patientId);
            LoadAvailableBedsAsync();
            LoadBloodTypesAsync();
        }
        private async void LoadBloodTypesAsync()
        {
            try
            {
                BloodTypes = await _bloodService.GetAllBloodTypesAsync();
                Debug.WriteLine($"Za�adowane grupy krwi: {string.Join(", ", BloodTypes.Select(bt => bt.Type))}");
                OnPropertyChanged(nameof(BloodTypes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading blood types: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� grup krwi.", "OK");
            }
        }
        private async void LoadPatient(int patientId)
        {
            try
            {
                var patient = await _databaseService.GetPatientByIdAsync(patientId);
                if (patient != null)
                {
                    SelectedPatient = patient;
                    Debug.WriteLine($"Za�adowano pacjenta: {patient.Name}, Grupa krwi: {patient.BloodType?.Type}, ��ko: {patient.BedNumber}");
                    OnPropertyChanged(nameof(SelectedPatient));
                }
                else
                {
                    Debug.WriteLine("Nie uda�o si� za�adowa� pacjenta.");
                    await DisplayAlert("B��d", "Nie uda�o si� za�adowa� danych pacjenta.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading patient: {ex.Message}");
            }
        }

        private async void LoadAvailableBedsAsync()
        {
            try
            {
                var availableBeds = await _bedService.GetAvailableBedsAsync();

                // Dodaj aktualne ��ko pacjenta do listy dost�pnych ��ek, je�li istnieje i nie jest ju� na li�cie
                if (SelectedPatient.BedNumber.HasValue && !availableBeds.Contains(SelectedPatient.BedNumber.Value))
                {
                    availableBeds.Add(SelectedPatient.BedNumber.Value);
                }

                AvailableBeds = availableBeds.OrderBy(b => b).ToList(); // Sortowanie dla czytelno�ci
                Debug.WriteLine($"Dost�pne ��ka: {string.Join(", ", AvailableBeds)}");
                OnPropertyChanged(nameof(AvailableBeds));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania dost�pnych ��ek: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� dost�pnych ��ek.", "OK");
            }

        }


        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine($"Zapisujemy pacjenta: {SelectedPatient.Name}, Grupa krwi: {SelectedPatient.BloodType?.Type}, ��ko: {SelectedPatient.BedNumber}");
                await _databaseService.SavePatientAsync(SelectedPatient);
                await DisplayAlert("Sukces", "Dane pacjenta zosta�y zapisane.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving patient: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� zapisa� danych pacjenta.", "OK");
            }
        }




        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
