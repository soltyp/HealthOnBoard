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
            "Dodanie wynik�w bada�",
            "Aktualizacja leczenia",
            "Podanie lek�w",
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
                await DisplayAlert("B��d", "Prosz� wybra� typ akcji.", "OK");
                return;
            }

            if (selectedActionType == "Pomiar temperatury")
            {
                if (string.IsNullOrWhiteSpace(TemperatureEntry.Text))
                {
                    await DisplayAlert("B��d", "Prosz� wprowadzi� temperatur�.", "OK");
                    return;
                }

                if (!decimal.TryParse(TemperatureEntry.Text, out decimal temperature)) // U�yj decimal zamiast float
                {
                    await DisplayAlert("B��d", "Wprowadzona temperatura musi by� liczb�.", "OK");
                    return;
                }

                // Aktualizacja temperatury w tabeli Patients
                bool patientUpdateSuccess = await _databaseService.UpdatePatientTemperatureAsync(_patientId, temperature);

                if (patientUpdateSuccess)
                {
                    // Dodanie wpisu do tabeli PatientActivityLog
                    string activityDetails = $"Zmierzono temperatur�: {temperature}�C";
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
                        await DisplayAlert("Sukces", "Temperatura zosta�a zaktualizowana i zapisano akcj�.", "OK");
                        _onActionAdded?.Invoke();
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("B��d", "Nie uda�o si� zapisa� akcji w logu.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� temperatury pacjenta.", "OK");
                }

                return;
            }


            // Obs�uga innych typ�w akcji
            if (string.IsNullOrWhiteSpace(actionDetails))
            {
                await DisplayAlert("B��d", "Prosz� wprowadzi� szczeg�y akcji.", "OK");
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
                await DisplayAlert("Sukces", "Akcja zosta�a dodana pomy�lnie.", "OK");
                _onActionAdded?.Invoke();
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("B��d", "Nie uda�o si� doda� akcji.", "OK");
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
