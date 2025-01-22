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
                Debug.WriteLine($"B��d w LoadBloodTypesAsync: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� grup krwi.", "OK");
            }
        }
        private async Task LoadPatientsAsync()
        {
            try
            {
                // �adujemy pacjent�w, kt�rzy s� widoczni
                var patients = await _databaseService.GetVisiblePatientsAsync();
                Debug.WriteLine($"Za�adowano pacjent�w: {patients.Count()}");

                // Czy�cimy list� pacjent�w w UI
                Patients.Clear();

                // Iterujemy po pacjentach i dodajemy ich do listy
                foreach (var patient in patients)
                {
                    Debug.WriteLine($"Pacjent: {patient.Name}, IsVisible2: {patient.IsVisible2}");

                    // Dodajemy pacjent�w, kt�rzy s� widoczni
                    Patients.Add(patient);
                }

                Debug.WriteLine($"Liczba pacjent�w wy�wietlanych w UI: {Patients.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania pacjent�w: {ex.Message}");
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
                // Zapisz nowego pacjenta do bazy danych
                await _databaseService.AddPatientAsync(NewPatient);

                // Dodaj pacjenta do listy w UI
                Patients.Add(NewPatient);

                // Wy�wietl komunikat sukcesu
                await DisplayAlert("Sukces", "Pacjent zosta� dodany.", "OK");

                // Wyczy�� dane w formularzu
                NewPatient = new Patient();
                OnPropertyChanged(nameof(NewPatient));

                // Od�wie� list� dost�pnych ��ek
                await LoadAvailableBedsAsync();
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
                        // Ustaw IsVisible2 na 0 w bazie danych (widoczny jako "ukryty")
                        patient.IsVisible2 = 0; // Zmieniamy na 0 (dla nie-widocznych pacjent�w)

                        // Zaktualizuj pacjenta w bazie danych
                        await _databaseService.UpdatePatientAsync(patient);

                        // Usu� pacjenta z listy w UI
                        Patients.Remove(patient);

                        await DisplayAlert("Sukces", "Pacjent zosta� usuni�ty.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error hiding patient: {ex.Message}");
                        await DisplayAlert("B��d", "Nie uda�o si� ukry� pacjenta.", "OK");
                    }
                }
            }
        }


    }
}
