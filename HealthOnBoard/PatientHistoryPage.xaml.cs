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
                string newActionType = await DisplayActionSheet(
                    "Zmieñ typ akcji",
                    "Anuluj",
                    null,
                    "Dodanie wyników badañ",
                    "Aktualizacja leczenia",
                    "Zmiana danych pacjenta",
                    "Podanie leków",
                    "Dodanie komentarza"
                );

                if (!string.IsNullOrWhiteSpace(newActionType) && newActionType != "Anuluj")
                {
                    activity.ActionType = newActionType;
                }

                string newActionDetails = await DisplayPromptAsync(
                    "Edytuj szczegó³y czynnoœci",
                    "Zmieñ szczegó³y czynnoœci:",
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
                            await DisplayAlert("Sukces", "Czynnoœæ zosta³a zaktualizowana.", "OK");
                            await LoadPatientHistoryAsync();
                        }
                        else
                        {
                            await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ czynnoœci.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B³¹d podczas edytowania czynnoœci: {ex.Message}");
                        await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas edycji czynnoœci.", "OK");
                    }
                }
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
