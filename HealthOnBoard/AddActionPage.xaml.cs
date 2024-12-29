using HospitalManagementData;
using System.Collections.ObjectModel;

namespace HealthOnBoard
{
    public partial class AddActionPage : ContentPage
    {
        private readonly int _patientId;
        private readonly int _userId;
        private readonly DatabaseService _databaseService;
        private readonly Action _onActionAdded;
        private int QuantityValue { get; set; } = 1;

        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> Units { get; set; } = new ObservableCollection<string> { "mg", "sztuka", "tablet" };
        public ObservableCollection<string> ActionTypes { get; set; } = new ObservableCollection<string>
        {
            "Dodanie wyników badañ",
            "Aktualizacja leczenia",
            "Podanie leków",
            "Dodanie komentarza",
            "Pomiar temperatury"
        };

        public AddActionPage(int patientId, int userId, DatabaseService databaseService, Action onActionAdded)
        {
            InitializeComponent();
            _patientId = patientId;
            _userId = userId;
            _databaseService = databaseService;
            _onActionAdded = onActionAdded;
            BindingContext = this;

            LoadMedications();
        }

        private async void LoadMedications()
        {
            var medications = await _databaseService.GetMedicationsAsync();
            foreach (var medication in medications)
            {
                Medications.Add(medication);
            }
        }

        private void OnIncreaseQuantityClicked(object sender, EventArgs e)
        {
            QuantityValue++;
            UpdateQuantityLabel();
        }

        private void OnDecreaseQuantityClicked(object sender, EventArgs e)
        {
            if (QuantityValue > 1)
            {
                QuantityValue--;
                UpdateQuantityLabel();
            }
        }

        private void UpdateQuantityLabel()
        {
            QuantityLabel.Text = $"Iloœæ: {QuantityValue}";
        }

        private void OnPresetQuantityClicked(object sender, EventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Text, out int quantity))
            {
                QuantityValue = quantity;
                UpdateQuantityLabel();
            }
        }

        private async void OnSaveActionClicked(object sender, EventArgs e)
        {
            string selectedActionType = ActionTypePicker.SelectedItem as string;
            string actionDetails = ActionDetailsEditor.Text;

            if (string.IsNullOrWhiteSpace(selectedActionType))
            {
                await DisplayAlert("B³¹d", "Proszê wybraæ typ akcji.", "OK");
                return;
            }

            if (selectedActionType == "Pomiar temperatury")
            {
                if (string.IsNullOrWhiteSpace(TemperatureEntry.Text))
                {
                    await DisplayAlert("B³¹d", "Proszê wprowadziæ temperaturê.", "OK");
                    return;
                }

                if (!decimal.TryParse(TemperatureEntry.Text, out decimal temperature))
                {
                    await DisplayAlert("B³¹d", "Wprowadzona temperatura musi byæ liczb¹.", "OK");
                    return;
                }

                bool patientUpdateSuccess = await _databaseService.UpdatePatientTemperatureAsync(_patientId, temperature);

                if (patientUpdateSuccess)
                {
                    string activityDetails = $"Zmierzono temperaturê: {temperature}°C";
                    bool logSuccess = await _databaseService.AddPatientActivityLogAsync(
                        userId: _userId,
                        patientId: _patientId,
                        actionType: selectedActionType,
                        actionDetails: activityDetails,
                        actionDate: DateTime.Now,
                        currentTemperature: temperature
                    );

                    if (logSuccess)
                    {
                        await DisplayAlert("Sukces", "Temperatura zosta³a zaktualizowana i zapisano akcjê.", "OK");
                        _onActionAdded?.Invoke();
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ akcji w logu.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ temperatury pacjenta.", "OK");
                }

                return;
            }

            if (selectedActionType == "Podanie leków")
            {
                var selectedMedication = MedicationPicker.SelectedItem as Medication;
                var selectedUnit = UnitPicker.SelectedItem as string;

                if (selectedMedication == null || selectedUnit == null)
                {
                    await DisplayAlert("B³¹d", "Proszê wybraæ lek i jednostkê.", "OK");
                    return;
                }

                string medicationDetails = $"{selectedMedication.Name}, Iloœæ: {QuantityValue} {selectedUnit}";

                bool medicationSuccess = await _databaseService.AddPatientActionAsync(
                    userID: _userId,
                    patientID: _patientId,
                    actionType: selectedActionType,
                    actionDetails: medicationDetails,
                    actionDate: DateTime.Now
                );

                if (medicationSuccess)
                {
                    await DisplayAlert("Sukces", "Podanie leków zosta³o zapisane pomyœlnie.", "OK");
                    _onActionAdded?.Invoke();
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ podania leków.", "OK");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(actionDetails))
            {
                await DisplayAlert("B³¹d", "Proszê wprowadziæ szczegó³y akcji.", "OK");
                return;
            }

            bool success = await _databaseService.AddPatientActionAsync(
                userID: _userId,
                patientID: _patientId,
                actionType: selectedActionType,
                actionDetails: actionDetails,
                actionDate: DateTime.Now
            );

            if (success)
            {
                await DisplayAlert("Sukces", "Akcja zosta³a dodana pomyœlnie.", "OK");
                _onActionAdded?.Invoke();
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê dodaæ akcji.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnActionTypeChanged(object sender, EventArgs e)
        {
            var selectedActionType = ActionTypePicker.SelectedItem as string;
            TemperatureEntry.IsVisible = selectedActionType == "Pomiar temperatury";
            MedicationControls.IsVisible = selectedActionType == "Podanie leków";
        }
    }
}
