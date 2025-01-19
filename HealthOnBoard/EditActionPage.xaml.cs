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

            // Ustaw widocznoœæ sekcji
            IsDrugAdministrationVisible = _activity.ActionType == "Podanie leków";
            IsTemperatureInputVisible = _activity.ActionType == "Pomiar temperatury";
            IsDetailsInputVisible = !_activity.ActionType.Contains("Pomiar temperatury") && !_activity.ActionType.Contains("Podanie leków");

            // Ustaw wartoœci pocz¹tkowe
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

        

        private void PopulateMedicationFields()
        {
            if (string.IsNullOrEmpty(_activity.ActionDetails))
            {
                Debug.WriteLine("Brak szczegó³ów dla Podania Leków.");
                return;
            }

            try
            {
                var actionDetails = _activity.ActionDetails.Split(',');
                if (actionDetails.Length > 1)
                {
                    var medicationName = actionDetails[0].Replace("Podano lek:", "").Trim();
                    var quantityUnit = actionDetails[1].Split(':');

                    // Przypisz wybrany lek na podstawie nazwy
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name.Equals(medicationName, StringComparison.OrdinalIgnoreCase));

                    // Przypisz iloœæ i jednostkê
                    SelectedQuantity = int.TryParse(quantityUnit[1].Split(' ')[1], out var quantity) ? quantity : 1;
                    SelectedUnit = quantityUnit[1].Split(' ')[2];

                    Debug.WriteLine($"Lek: {SelectedMedication?.Name}, Iloœæ: {SelectedQuantity}, Jednostka: {SelectedUnit}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas wype³niania pól Podania Leków: {ex.Message}");
            }
        }

        private async void LoadMedicationsAsync()
        {
            try
            {
                var medications = await _databaseService.GetMedicationsAsync();
                foreach (var medication in medications)
                {
                    Medications.Add(medication);
                }

                // Po za³adowaniu listy leków, spróbuj ustawiæ pocz¹tkowe wartoœci
                PopulateMedicationFields();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania leków: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ listy leków.", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Przypisz odpowiednie wartoœci na podstawie typu akcji
                if (_activity.ActionType == "Pomiar temperatury" && decimal.TryParse(TemperatureEntry.Text, out var temperature))
                {
                    _activity.CurrentTemperature = temperature;
                }
                else if (_activity.ActionType == "Podanie leków" && SelectedMedication != null)
                {
                    _activity.ActionDetails = $"Podano lek: {SelectedMedication.Name}, iloœæ: {SelectedQuantity} {SelectedUnit}";
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
                    Debug.WriteLine("Zapisano zmiany, wysy³anie powiadomienia o aktualizacji historii...");

                    // Wys³anie komunikatu o aktualizacji
                    MessagingCenter.Send(this, "RefreshPatientActivityHistory");

                    await DisplayAlert("Sukces", "Dane zosta³y zapisane.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ danych.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas zapisu: {ex.Message}");
                await DisplayAlert("B³¹d", $"Wyst¹pi³ problem: {ex.Message}", "OK");
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
