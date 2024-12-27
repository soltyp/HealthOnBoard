using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HealthOnBoard
{
    public partial class ManagePatientsPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<Patient> Patients { get; set; }
        public Patient NewPatient { get; set; }
        private readonly DatabaseService _databaseService;

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

        public string ToggleAddSectionText => IsAddSectionVisible ? "Zwiñ Sekcjê" : "Rozwiñ Sekcjê";

        public ManagePatientsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            Patients = new ObservableCollection<Patient>();
            NewPatient = new Patient();
            BindingContext = this;

            IsAddSectionVisible = false; // Domyœlnie sekcja ukryta
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPatientsAsync();
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

        private async void OnAddPatientClicked(object sender, EventArgs e)
        {
            try
            {
                await _databaseService.AddPatientAsync(NewPatient);
                await DisplayAlert("Sukces", "Pacjent zosta³ dodany.", "OK");
                await LoadPatientsAsync(); // Odœwie¿ listê pacjentów
                NewPatient = new Patient(); // Wyczyœæ pola
                OnPropertyChanged(nameof(NewPatient));
            }
            catch (InvalidOperationException ex)
            {
                await DisplayAlert("B³¹d", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas dodawania pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê dodaæ pacjenta.", "OK");
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
                await Navigation.PushAsync(new EditPatientPage(patient.PatientID, _databaseService));
            }
        }

        private async void OnDeletePatientClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient patient)
            {
                bool confirm = await DisplayAlert("Potwierdzenie", $"Czy na pewno chcesz usun¹æ pacjenta {patient.Name}?", "Tak", "Nie");
                if (confirm)
                {
                    try
                    {
                        await _databaseService.DeletePatientAsync(patient.PatientID);
                        Patients.Remove(patient);
                        await DisplayAlert("Sukces", "Pacjent zosta³ usuniêty.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting patient: {ex.Message}");
                        await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ pacjenta.", "OK");
                    }
                }
            }
        }
    }
}
