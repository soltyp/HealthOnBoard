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
            

            InitializePage(patientId);
        }

        private async void InitializePage(int patientId)
        {
            await LoadBloodTypesAsync();
            Debug.WriteLine($"BloodTypes za�adowane: {string.Join(", ", BloodTypes.Select(bt => bt.Type))}");

            await LoadAvailableBedsAsync();
            Debug.WriteLine($"AvailableBeds za�adowane: {string.Join(", ", AvailableBeds)}");

            await LoadPatient(patientId);
            Debug.WriteLine($"Pacjent za�adowany: {SelectedPatient.Name}, Grupa krwi: {SelectedPatient.BloodType?.Type}");

            BindingContext = this; // Ustaw kontekst po za�adowaniu danych
            Debug.WriteLine($"BindingContext ustawiony: {BindingContext?.GetType().Name}");
        }



        private async Task LoadBloodTypesAsync()
        {
            try
            {
                BloodTypes = await _bloodService.GetAllBloodTypesAsync();
                Debug.WriteLine($"Za�adowane grupy krwi: {string.Join(", ", BloodTypes.Select(bt => bt.Type))}");
                OnPropertyChanged(nameof(BloodTypes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d w LoadBloodTypesAsync: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� grup krwi.", "OK");
            }
        }



        private async Task LoadPatient(int patientId)
        {
            try
            {
                var patient = await _databaseService.GetPatientByIdAsync(patientId);
                if (patient != null)
                {
                    SelectedPatient = patient;

                    // Dopasowanie obiektu BloodType z listy BloodTypes
                    if (SelectedPatient.BloodTypeID.HasValue)
                    {
                        SelectedPatient.BloodType = BloodTypes
                            .FirstOrDefault(bt => bt.BloodTypeID == SelectedPatient.BloodTypeID.Value)
                            ?? new BloodType { Type = "Nieznana grupa krwi" };
                    }
                    else if (!string.IsNullOrEmpty(SelectedPatient.PatientBloodType))
                    {
                        SelectedPatient.BloodType = BloodTypes
                            .FirstOrDefault(bt => bt.Type == SelectedPatient.PatientBloodType)
                            ?? new BloodType { Type = SelectedPatient.PatientBloodType };
                    }
                    else
                    {
                        SelectedPatient.BloodType = new BloodType { Type = "Brak danych" };
                    }

                    Debug.WriteLine($"Pacjent za�adowany: {SelectedPatient.Name}, Grupa krwi: {SelectedPatient.BloodType?.Type}");
                    OnPropertyChanged(nameof(SelectedPatient));
                }
                else
                {
                    await DisplayAlert("B��d", "Nie znaleziono danych pacjenta.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d w LoadPatient: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� pacjenta.", "OK");
            }
        }


        private async void OnRemoveBedClicked(object sender, EventArgs e)
        {
            try
            {
                // Ustawienie BedNumber na null
                SelectedPatient.BedNumber = null;
                OnPropertyChanged(nameof(SelectedPatient));

                // Aktualizacja w bazie danych
                await _databaseService.SavePatientAsync(SelectedPatient);

                await DisplayAlert("Sukces", "Numer ��ka zosta� usuni�ty.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas usuwania numeru ��ka: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� usun�� numeru ��ka.", "OK");
            }
        }



        private async Task LoadAvailableBedsAsync()
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
