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

        public string ToggleAddSectionText => IsAddSectionVisible ? "Zwi� Sekcj�" : "Rozwi� Sekcj�";

        public ManagePatientsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            Patients = new ObservableCollection<Patient>();
            NewPatient = new Patient();
            BindingContext = this;

            IsAddSectionVisible = false; // Domy�lnie sekcja ukryta
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
                await DisplayAlert("Sukces", "Pacjent zosta� dodany.", "OK");
                await LoadPatientsAsync(); // Od�wie� list� pacjent�w
                NewPatient = new Patient(); // Wyczy�� pola
                OnPropertyChanged(nameof(NewPatient));
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
                await Navigation.PushAsync(new EditPatientPage(patient.PatientID, _databaseService));
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
