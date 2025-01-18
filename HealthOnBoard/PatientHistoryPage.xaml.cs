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
            _dashboardPage = dashboardPage; // Przechowujemy instancjê DashboardPage
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
                Debug.WriteLine("Rozpoczynam ³adowanie historii pacjenta...");
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);
                Debug.WriteLine($"Pobrano {activities.Count} rekordów historii pacjenta.");

                PatientHistory.Clear();
                foreach (var activity in activities)
                {
                    activity.PatientID = _patientId; // Ustawienie PatientID
                    Debug.WriteLine($"Dodawanie czynnoœci: {activity.ActionType}, {activity.ActionDetails}, {activity.ActionDate}, PatientID: {activity.PatientID}");
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê pobraæ historii pacjenta.", "OK");
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
                        // Pobierz bie¿¹c¹ temperaturê z logu
                        var existingTemperature = await _databaseService.GetCurrentTemperatureAsync(activity.PatientID);

                        if (existingTemperature == null)
                        {
                            await DisplayAlert("B³¹d", "Nie znaleziono wartoœci temperatury w logu bazy danych.", "OK");
                            return;
                        }

                        // Wyœwietl dialog do edycji temperatury
                        string newTemperature = await DisplayPromptAsync(
                            "Edytuj pomiar temperatury",
                            "WprowadŸ now¹ wartoœæ temperatury (w °C):",
                            keyboard: Keyboard.Numeric,
                            initialValue: existingTemperature.Value.ToString("F1")
                        );

                        // Walidacja temperatury
                        if (decimal.TryParse(newTemperature, out decimal CurrentTemperature) && CurrentTemperature >= 35 && CurrentTemperature <= 42)
                        {
                            // Zaktualizuj temperaturê w bazie
                            bool updateSuccess = await _databaseService.UpdateActivityLogTemperatureAsync(activity.LogID, CurrentTemperature);

                            if (updateSuccess)
                            {
                                await DisplayAlert("Sukces", "Pomiar temperatury zosta³ zaktualizowany.", "OK");

                                // Odœwie¿ dane w widoku
                                await LoadPatientHistoryAsync();
                                await _dashboardPage.LoadRecentActivitiesAsync();

                                // Odœwie¿ wykres
                                await _dashboardPage.LoadTemperatureChartDataAsync();
                                await _dashboardPage.LoadPatientTemperatureAsync();
                            }
                            else
                            {
                                Debug.WriteLine($"Nie uda³o siê zaktualizowaæ danych logu dla LogID={activity.LogID}.");
                                await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ danych w logu.", "OK");
                            }
                        }
                        else
                        {
                            await DisplayAlert("B³¹d", "WprowadŸ poprawn¹ wartoœæ temperatury (35-42°C).", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B³¹d podczas edycji temperatury: {ex.Message}");
                        await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas edycji temperatury.", "OK");
                    }
                }
            }
        }






        private async Task RefreshPatientHistoryAsync()
        {
            try
            {
                // Pobierz zaktualizowan¹ historiê pacjenta
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);

                // Wyczyœæ bie¿¹c¹ listê i za³aduj nowe dane
                PatientHistory.Clear();
                foreach (var activity in activities)
                {
                    PatientHistory.Add(activity);
                }

                Debug.WriteLine("Historia pacjenta zosta³a odœwie¿ona.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas odœwie¿ania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê odœwie¿yæ historii pacjenta.", "OK");
            }
        }


       



        private async void OnDeleteActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                Debug.WriteLine($"Rozpoczynam usuwanie czynnoœci: {activity.ActionType}, {activity.ActionDetails}, {activity.ActionDate}, PatientID: {activity.PatientID}");

                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun¹æ tê czynnoœæ?", "Tak", "Nie");
                if (!confirmDelete)
                {
                    Debug.WriteLine("Usuwanie anulowane przez u¿ytkownika.");
                    return;
                }

                try
                {
                    Debug.WriteLine("Wywo³ywanie metody DeletePatientActionAsync...");
                    bool success = await _databaseService.DeletePatientActionAsync(activity);

                    if (success)
                    {
                        Debug.WriteLine("Czynnoœæ zosta³a usuniêta z bazy danych.");
                        Debug.WriteLine($"Przed usuniêciem: Liczba elementów w PatientHistory: {PatientHistory.Count}");
                        PatientHistory.Remove(activity);
                        Debug.WriteLine($"Po usuniêciu: Liczba elementów w PatientHistory: {PatientHistory.Count}");

                        await DisplayAlert("Sukces", "Czynnoœæ zosta³a usuniêta.", "OK");
                    }
                    else
                    {
                        Debug.WriteLine("Metoda DeletePatientActionAsync zwróci³a false. Czynnoœæ nie zosta³a usuniêta.");
                        await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ czynnoœci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B³¹d podczas usuwania czynnoœci: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas usuwania czynnoœci.", "OK");
                }
            }
            else
            {
                Debug.WriteLine("Nie uda³o siê pobraæ danych czynnoœci z CommandParameter.");
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
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
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
