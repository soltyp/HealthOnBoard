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
            _bedService = bedService;
            _bloodService = bloodService;

            SelectedPatient = new Patient();
            AvailableBeds = new List<int>();
            BloodTypes = new List<BloodType>();
            BindingContext = this;

            LoadBloodTypesAsync();
            LoadAvailableBedsAsync();
            LoadPatient(patientId);
        }
        private async void LoadBloodTypesAsync()
{
    try
    {
        // Pobierz listê grup krwi z bazy danych
        BloodTypes = await _bloodService.GetAllBloodTypesAsync();

        Debug.WriteLine($"Za³adowane grupy krwi: {string.Join(", ", BloodTypes.Select(bt => bt.Type))}");
        OnPropertyChanged(nameof(BloodTypes));
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error loading blood types: {ex.Message}");
        await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ grup krwi.", "OK");
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

                    // SprawdŸ, czy BloodTypeID jest null lub pusty, jeœli tak - nie ³aduj
                    if (SelectedPatient.BloodTypeID.HasValue)
                    {
                        var bloodType = await _databaseService.GetBloodTypeByIdAsync(SelectedPatient.BloodTypeID.Value);
                        if (bloodType != null)
                        {
                            // Przypisz grupê krwi do pacjenta
                            SelectedPatient.BloodType = bloodType;
                        }
                        else
                        {
                            SelectedPatient.BloodType = null; // Jeœli nie znaleziono, ustaw na null
                        }
                    }

                    Debug.WriteLine($"Za³adowano pacjenta: {patient.Name}, Grupa krwi: {SelectedPatient.BloodType?.Type}, £ó¿ko: {SelectedPatient.BedNumber}");
                    OnPropertyChanged(nameof(SelectedPatient));
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ danych pacjenta.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d w LoadPatient: {ex.Message}");
                throw;
            }
        }





        private async void LoadAvailableBedsAsync()
        {
            try
            {
                var availableBeds = await _bedService.GetAvailableBedsAsync();

                // Dodaj aktualne ³ó¿ko pacjenta do listy dostêpnych ³ó¿ek, jeœli istnieje i nie jest ju¿ na liœcie
                if (SelectedPatient.BedNumber.HasValue && !availableBeds.Contains(SelectedPatient.BedNumber.Value))
                {
                    availableBeds.Add(SelectedPatient.BedNumber.Value);
                }

                AvailableBeds = availableBeds.OrderBy(b => b).ToList(); // Sortowanie dla czytelnoœci
                Debug.WriteLine($"Dostêpne ³ó¿ka: {string.Join(", ", AvailableBeds)}");
                OnPropertyChanged(nameof(AvailableBeds));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania dostêpnych ³ó¿ek: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ dostêpnych ³ó¿ek.", "OK");
            }

        }


        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine($"Zapisujemy pacjenta: {SelectedPatient.Name}, Grupa krwi: {SelectedPatient.BloodType?.Type}, £ó¿ko: {SelectedPatient.BedNumber}");
                await _databaseService.SavePatientAsync(SelectedPatient);
                await DisplayAlert("Sukces", "Dane pacjenta zosta³y zapisane.", "OK");
                await Navigation.PopAsync();
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
