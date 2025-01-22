using HospitalManagementData;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class EditActionPage : ContentPage
    {
        private readonly PatientActivity _activity;
        private readonly DatabaseService _databaseService;

        public string ActionDetails { get; set; }

        public string ActionType { get; set; }
        public bool IsTemperatureInputVisible { get; set; }
        public bool IsDetailsInputVisible { get; set; }
        public string CurrentTemperature { get; set; }
        public bool IsDrugAdministrationVisible { get; set; }

        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> MedicationUnits { get; set; } = new ObservableCollection<string> { "sztuka", "ml", "mg" };
        public ObservableCollection<Medication> FilteredMedications { get; set; } = new ObservableCollection<Medication>();


        private Medication _selectedMedication;
        public Medication SelectedMedication
        {
            get => _selectedMedication;
            set
            {
                _selectedMedication = value;
                OnPropertyChanged(nameof(SelectedMedication));
            }
        }

        private string _selectedUnit = "sztuka";
        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged(nameof(SelectedUnit));
            }
        }

        private int _selectedQuantity = 1;
        public int SelectedQuantity
        {
            get => _selectedQuantity;
            set
            {
                _selectedQuantity = value;
                OnPropertyChanged(nameof(SelectedQuantity));
            }
        }

        public EditActionPage(PatientActivity activity, DatabaseService databaseService)
        {
            InitializeComponent();

            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Ustaw widoczno�� sekcji
            IsDrugAdministrationVisible = _activity.ActionType == "Podanie lek�w";
            IsTemperatureInputVisible = _activity.ActionType == "Pomiar temperatury";
            IsDetailsInputVisible = !_activity.ActionType.Contains("Pomiar temperatury") && !_activity.ActionType.Contains("Podanie lek�w");

            // Ustaw warto�ci pocz�tkowe
            ActionType = _activity.ActionType;
            ActionDetails = _activity.ActionDetails;
            CurrentTemperature = _activity.CurrentTemperature?.ToString("F1") ?? string.Empty;

            if (IsDrugAdministrationVisible)
            {
                LoadMedicationsAsync();
                PopulateMedicationFields();
            }

            BindingContext = this;
        }



        public string PreviousMedication { get; set; } = "Brak"; // Domy�lnie "Brak"

        private void PopulateMedicationFields()
        {
            if (string.IsNullOrEmpty(_activity.ActionDetails))
            {
                Debug.WriteLine("Brak szczeg��w dla Podania Lek�w.");
                return;
            }

            try
            {
                var actionDetails = _activity.ActionDetails.Split(',');
                if (actionDetails.Length > 1)
                {
                    var medicationName = actionDetails[0].Replace("Podano lek:", "").Trim();
                    var quantityUnit = actionDetails[1].Split(':');

                    // Przypisz nazw� istniej�cego leku do w�a�ciwo�ci
                    PreviousMedication = medicationName;

                    // Ustaw lek na domy�lny w Pickerze
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name.Equals(medicationName, StringComparison.OrdinalIgnoreCase));

                    // Je�li lek nie znajduje si� w przefiltrowanej li�cie, dodaj go tymczasowo
                    if (SelectedMedication != null && !FilteredMedications.Contains(SelectedMedication))
                    {
                        FilteredMedications.Add(SelectedMedication);
                    }

                    // Przypisz ilo�� i jednostk�
                    SelectedQuantity = int.TryParse(quantityUnit[1].Split(' ')[1], out var quantity) ? quantity : 1;
                    SelectedUnit = quantityUnit[1].Split(' ')[2];

                    Debug.WriteLine($"Lek: {PreviousMedication}, Ilo��: {SelectedQuantity}, Jednostka: {SelectedUnit}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas wype�niania p�l Podania Lek�w: {ex.Message}");
            }
        }



        private async void LoadMedicationsAsync()
        {
            try
            {
                var medications = await _databaseService.GetMedicationsAsync();
                Medications.Clear();
                FilteredMedications.Clear();

                foreach (var medication in medications)
                {
                    Medications.Add(medication);
                    FilteredMedications.Add(medication); // Na pocz�tku pokazujemy ca�� list�
                }

                // Je�li lek jest przypisany, ustaw go jako wybrany
                if (!string.IsNullOrEmpty(PreviousMedication))
                {
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name.Equals(PreviousMedication, StringComparison.OrdinalIgnoreCase));
                }

                PopulateMedicationFields();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania lek�w: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� listy lek�w.", "OK");
            }
        }


        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Przypisz odpowiednie warto�ci na podstawie typu akcji
                if (_activity.ActionType == "Pomiar temperatury" && decimal.TryParse(TemperatureEntry.Text, out var temperature))
                {
                    _activity.CurrentTemperature = temperature;
                }
                else if (_activity.ActionType == "Podanie lek�w" && SelectedMedication != null)
                {
                    _activity.ActionDetails = $"Podano lek: {SelectedMedication.Name}, ilo��: {SelectedQuantity} {SelectedUnit}";
                }
                else
                {
                    _activity.ActionDetails = ActionDetails;
                }

                // Zaktualizuj w bazie danych
                var success = await _databaseService.UpdateActivityLogAsync(
                    _activity.LogID,
                    _activity.ActionType,
                    _activity.ActionDetails,
                    _activity.CurrentTemperature
                );

                if (success)
                {
                    Debug.WriteLine("Zapisano zmiany, wysy�anie powiadomienia o aktualizacji historii...");

                    // Wys�anie komunikatu o aktualizacji
                    MessagingCenter.Send(this, "RefreshPatientActivityHistory");

                    await DisplayAlert("Sukces", "Dane zosta�y zapisane.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� danych.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas zapisu: {ex.Message}");
                await DisplayAlert("B��d", $"Wyst�pi� problem: {ex.Message}", "OK");
            }
        }
        private void OnAlphabetFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.Text is not null)
            {
                string selectedLetter = button.Text;

                // Filtrowanie lek�w na podstawie wybranej litery
                FilteredMedications.Clear();

                foreach (var medication in Medications)
                {
                    if (medication.Name.StartsWith(selectedLetter, StringComparison.OrdinalIgnoreCase))
                    {
                        FilteredMedications.Add(medication);
                    }
                }

                Debug.WriteLine($"Filtered medications by letter: {selectedLetter}. Found {FilteredMedications.Count} medications.");

                // Automatycznie wybierz pierwszy lek z listy, je�li istnieje
                SelectedMedication = FilteredMedications.FirstOrDefault();
            }
        }



        private void DecreaseQuantity_Clicked(object sender, EventArgs e)
        {
            if (SelectedQuantity > 1)
            {
                SelectedQuantity--;
            }
        }

        private void IncreaseQuantity_Clicked(object sender, EventArgs e)
        {
            SelectedQuantity++;
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
