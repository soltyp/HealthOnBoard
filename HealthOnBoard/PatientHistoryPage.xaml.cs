using HospitalManagementData;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientHistoryPage : ContentPage
    {
        private readonly User _user;
        private readonly int _patientId;
        private readonly DatabaseService _databaseService;
        public ObservableCollection<PatientActivity> PatientHistory { get; set; } = new ObservableCollection<PatientActivity>();
        private System.Timers.Timer _logoutTimer;
        private const int LogoutTimeInSeconds = 180; // 3 minutes
        private int _remainingTimeInSeconds;

        public PatientHistoryPage(User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            InitializeLogoutTimer();
            LoadPatientHistoryAsync();
            BindingContext = this;
        }

        private async Task LoadPatientHistoryAsync()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam �adowanie historii pacjenta...");
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);
                Debug.WriteLine($"Pobrano {activities.Count} rekord�w historii pacjenta.");

                PatientHistory.Clear();
                foreach (var activity in activities)
                {
                    activity.PatientID = _patientId; // Ustawienie PatientID
                    Debug.WriteLine($"Dodawanie czynno�ci: {activity.ActionType}, {activity.ActionDetails}, {activity.ActionDate}, PatientID: {activity.PatientID}");
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� pobra� historii pacjenta.", "OK");
            }
        }



        private async void OnEditActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                string newActionType = await DisplayActionSheet(
                    "Zmie� typ akcji",
                    "Anuluj",
                    null,
                    "Dodanie wynik�w bada�",
                    "Aktualizacja leczenia",
                    "Zmiana danych pacjenta",
                    "Podanie lek�w",
                    "Dodanie komentarza"
                );

                if (!string.IsNullOrWhiteSpace(newActionType) && newActionType != "Anuluj")
                {
                    activity.ActionType = newActionType;
                }

                string newActionDetails = await DisplayPromptAsync(
                    "Edytuj szczeg�y czynno�ci",
                    "Zmie� szczeg�y czynno�ci:",
                    initialValue: activity.ActionDetails
                );

                if (!string.IsNullOrWhiteSpace(newActionDetails))
                {
                    try
                    {
                        activity.ActionDetails = newActionDetails;
                        bool success = await _databaseService.UpdatePatientActionAsync(activity);

                        if (success)
                        {
                            await DisplayAlert("Sukces", "Czynno�� zosta�a zaktualizowana.", "OK");
                            await LoadPatientHistoryAsync();
                        }
                        else
                        {
                            await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� czynno�ci.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B��d podczas edytowania czynno�ci: {ex.Message}");
                        await DisplayAlert("B��d", "Wyst�pi� problem podczas edycji czynno�ci.", "OK");
                    }
                }
            }
        }

        private async void OnDeleteActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                Debug.WriteLine($"Rozpoczynam usuwanie czynno�ci: {activity.ActionType}, {activity.ActionDetails}, {activity.ActionDate}, PatientID: {activity.PatientID}");

                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun�� t� czynno��?", "Tak", "Nie");
                if (!confirmDelete)
                {
                    Debug.WriteLine("Usuwanie anulowane przez u�ytkownika.");
                    return;
                }

                try
                {
                    Debug.WriteLine("Wywo�ywanie metody DeletePatientActionAsync...");
                    bool success = await _databaseService.DeletePatientActionAsync(activity);

                    if (success)
                    {
                        Debug.WriteLine("Czynno�� zosta�a usuni�ta z bazy danych.");
                        Debug.WriteLine($"Przed usuni�ciem: Liczba element�w w PatientHistory: {PatientHistory.Count}");
                        PatientHistory.Remove(activity);
                        Debug.WriteLine($"Po usuni�ciu: Liczba element�w w PatientHistory: {PatientHistory.Count}");

                        await DisplayAlert("Sukces", "Czynno�� zosta�a usuni�ta.", "OK");
                    }
                    else
                    {
                        Debug.WriteLine("Metoda DeletePatientActionAsync zwr�ci�a false. Czynno�� nie zosta�a usuni�ta.");
                        await DisplayAlert("B��d", "Nie uda�o si� usun�� czynno�ci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B��d podczas usuwania czynno�ci: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await DisplayAlert("B��d", "Wyst�pi� problem podczas usuwania czynno�ci.", "OK");
                }
            }
            else
            {
                Debug.WriteLine("Nie uda�o si� pobra� danych czynno�ci z CommandParameter.");
            }
        }


        private void InitializeLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
            _logoutTimer = new System.Timers.Timer(1000);
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private void UpdateCountdown(object sender, System.Timers.ElapsedEventArgs e)
        {
            _remainingTimeInSeconds--;
            Dispatcher.Dispatch(() =>
            {
                int minutes = _remainingTimeInSeconds / 60;
                int seconds = _remainingTimeInSeconds % 60;
                LogoutTimer.Text = $"{minutes:D2}:{seconds:D2}";

                if (_remainingTimeInSeconds <= 0)
                {
                    _logoutTimer.Stop();
                    LogoutUser();
                }
            });
        }

        private async void LogoutUser()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Sesja wygas�a", "Twoja sesja wygas�a z powodu braku aktywno�ci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        private void OnScreenTapped(object sender, EventArgs e)
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
