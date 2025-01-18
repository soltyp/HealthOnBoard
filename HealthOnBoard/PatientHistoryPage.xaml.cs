using HospitalManagementData;
using Microsoft.Maui.Graphics;
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


        private readonly DashboardPage _dashboardPage;

    
       public PatientHistoryPage(DashboardPage dashboardPage, User user, int patientId, DatabaseService databaseService)
{
    InitializeComponent();
            _dashboardPage = dashboardPage; // Przechowujemy instancj� DashboardPage
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            InitializeLogoutTimer();
            LoadPatientHistoryAsync();
            BindingContext = this;
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");

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
                if (activity.ActionType == "Pomiar temperatury")
                {
                    try
                    {
                        // Pobierz bie��c� temperatur� z logu
                        var existingTemperature = await _databaseService.GetCurrentTemperatureAsync(activity.PatientID);

                        if (existingTemperature == null)
                        {
                            await DisplayAlert("B��d", "Nie znaleziono warto�ci temperatury w logu bazy danych.", "OK");
                            return;
                        }

                        // Wy�wietl dialog do edycji temperatury
                        string newTemperature = await DisplayPromptAsync(
                            "Edytuj pomiar temperatury",
                            "Wprowad� now� warto�� temperatury (w �C):",
                            keyboard: Keyboard.Numeric,
                            initialValue: existingTemperature.Value.ToString("F1")
                        );

                        // Walidacja temperatury
                        if (decimal.TryParse(newTemperature, out decimal CurrentTemperature) && CurrentTemperature >= 35 && CurrentTemperature <= 42)
                        {
                            // Zaktualizuj temperatur� w bazie
                            bool updateSuccess = await _databaseService.UpdateActivityLogTemperatureAsync(activity.LogID, CurrentTemperature);

                            if (updateSuccess)
                            {
                                await DisplayAlert("Sukces", "Pomiar temperatury zosta� zaktualizowany.", "OK");

                                // Od�wie� dane w widoku
                                await LoadPatientHistoryAsync();
                                await _dashboardPage.LoadRecentActivitiesAsync();

                                // Od�wie� wykres
                                await _dashboardPage.LoadTemperatureChartDataAsync();
                                await _dashboardPage.LoadPatientTemperatureAsync();
                            }
                            else
                            {
                                Debug.WriteLine($"Nie uda�o si� zaktualizowa� danych logu dla LogID={activity.LogID}.");
                                await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� danych w logu.", "OK");
                            }
                        }
                        else
                        {
                            await DisplayAlert("B��d", "Wprowad� poprawn� warto�� temperatury (35-42�C).", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B��d podczas edycji temperatury: {ex.Message}");
                        await DisplayAlert("B��d", "Wyst�pi� problem podczas edycji temperatury.", "OK");
                    }
                }
            }
        }






        private async Task RefreshPatientHistoryAsync()
        {
            try
            {
                // Pobierz zaktualizowan� histori� pacjenta
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);

                // Wyczy�� bie��c� list� i za�aduj nowe dane
                PatientHistory.Clear();
                foreach (var activity in activities)
                {
                    PatientHistory.Add(activity);
                }

                Debug.WriteLine("Historia pacjenta zosta�a od�wie�ona.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas od�wie�ania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� od�wie�y� historii pacjenta.", "OK");
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
