using System.Collections.ObjectModel;

namespace HealthOnBoard
{
    public partial class AddActionPage : ContentPage
    {
        private readonly int _patientId;
        private readonly int _userId;
        private readonly DatabaseService _databaseService;
        private readonly Action _onActionAdded;

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

                if (!decimal.TryParse(TemperatureEntry.Text, out decimal temperature)) // U¿yj decimal zamiast float
                {
                    await DisplayAlert("B³¹d", "Wprowadzona temperatura musi byæ liczb¹.", "OK");
                    return;
                }

                // Aktualizacja temperatury w tabeli Patients
                bool patientUpdateSuccess = await _databaseService.UpdatePatientTemperatureAsync(_patientId, temperature);

                if (patientUpdateSuccess)
                {
                    // Dodanie wpisu do tabeli PatientActivityLog
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


            // Obs³uga innych typów akcji
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
        }
    }
}
