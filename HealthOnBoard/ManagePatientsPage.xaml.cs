using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading.Tasks;
using HospitalManagementData;

namespace HealthOnBoard
{
    public partial class ManagePatientsPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<Patient> Patients { get; set; }
        public Patient NewPatient { get; set; }
        private readonly DatabaseService _databaseService;
        private readonly BedService _bedService;
        public List<BloodType> BloodTypes { get; set; }
        private readonly BloodService _bloodService;

        public ObservableCollection<int> AvailableBeds { get; set; } = new ObservableCollection<int>();

        private bool _isAddSectionVisible;
        public bool IsAddSectionVisible
        {
            get => _isAddSectionVisible;
            set
            {
                _isAddSectionVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleAddSectionText));
            }
        }

        public string ToggleAddSectionText => IsAddSectionVisible ? "Zwi� Sekcj�" : "Rozwi� Sekcj�";

        public ManagePatientsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _bedService = new BedService(databaseService); // Inicjalizacja BedService
            _bloodService = new BloodService(databaseService);
            Patients = new ObservableCollection<Patient>();
            NewPatient = new Patient();
            BindingContext = this;

            IsAddSectionVisible = false; // Domy�lnie sekcja ukryta
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPatientsAsync();
            await LoadAvailableBedsAsync();
            await LoadBloodTypesAsync();

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
                Debug.WriteLine($"Error loading blood types: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� grup krwi.", "OK");
            }
        }
        private async Task LoadPatientsAsync()
        {
            try
            {
                var patients = await _databaseService.GetPatientsAsync();
                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading patients: {ex.Message}");
            }
        }

        private async Task LoadAvailableBedsAsync()
        {
            try
            {
                var beds = await _bedService.GetAvailableBedsAsync();
                AvailableBeds.Clear();
                foreach (var bed in beds)
                {
                    AvailableBeds.Add(bed);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading beds: {ex.Message}");
            }
        }

        private async void OnAddPatientClicked(object sender, EventArgs e)
        {
            try
            {
                var availableBeds = await _bedService.GetAvailableBedsAsync();

                // Dodaj aktualne ��ko pacjenta do listy dost�pnych ��ek, je�li istnieje i nie jest ju� na li�cie
                if (NewPatient.BedNumber.HasValue && !availableBeds.Contains(NewPatient.BedNumber.Value))
                {
                    availableBeds.Add(NewPatient.BedNumber.Value);
                }

                // Przypisz dost�pne ��ka do w�a�ciwo�ci bindowanej w UI
                AvailableBeds = new ObservableCollection<int>(availableBeds.OrderBy(b => b));
                Debug.WriteLine($"Dost�pne ��ka: {string.Join(", ", AvailableBeds)}");
                OnPropertyChanged(nameof(AvailableBeds));
            }
            catch (InvalidOperationException ex)
            {
                await DisplayAlert("B��d", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas dodawania pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� doda� pacjenta.", "OK");
            }

        }

        private void OnToggleAddSectionClicked(object sender, EventArgs e)
        {
            IsAddSectionVisible = !IsAddSectionVisible;
        }

        private async void OnEditPatientClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient patient)
            {
                var bedService = new BedService(_databaseService);
                var bloodService = new BloodService(_databaseService);
                await Navigation.PushAsync(new EditPatientPage(patient.PatientID, _databaseService, bedService, bloodService));
            }
        }

        private async void OnDeletePatientClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient patient)
            {
                bool confirm = await DisplayAlert("Potwierdzenie", $"Czy na pewno chcesz usun�� pacjenta {patient.Name}?", "Tak", "Nie");
                if (confirm)
                {
                    try
                    {
                        await _databaseService.DeletePatientAsync(patient.PatientID);
                        Patients.Remove(patient);
                        await DisplayAlert("Sukces", "Pacjent zosta� usuni�ty.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting patient: {ex.Message}");
                        await DisplayAlert("B��d", "Nie uda�o si� usun�� pacjenta.", "OK");
                    }
                }
            }
        }
    }
}
