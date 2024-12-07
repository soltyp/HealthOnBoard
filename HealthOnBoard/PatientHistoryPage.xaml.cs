using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using HospitalManagementAPI.Models;
using HospitalManagementData;

namespace HealthOnBoard
{
    public partial class PatientHistoryPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly int _patientId;
        private readonly User _user;
        public ObservableCollection<PatientActivity> PatientHistory { get; set; } = new ObservableCollection<PatientActivity>();

        private System.Timers.Timer _logoutTimer;
        private int _remainingTimeInSeconds = 180; // 3 minutes

        public PatientHistoryPage(User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "DatabaseService cannot be null");

            // Set user data in navigation bar
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = _user.Role ?? "Brak danych";

            // Initialize timer
            InitializeLogoutTimer();

            // Add tap gesture for idle reset
            AddTapGestureToMainGrid();

            // Load patient history
            LoadPatientHistoryAsync();
            BindingContext = this;
        }

        private void InitializeLogoutTimer()
        {
            _logoutTimer = new System.Timers.Timer(1000); // 1 second
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private void UpdateCountdown(object sender, ElapsedEventArgs e)
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

        private async void LoadPatientHistoryAsync()
        {
            try
            {
                var history = await _databaseService.GetFullActivitiesAsync(_patientId);
                PatientHistory.Clear();

                foreach (var activity in history)
                {
                    PatientHistory.Add(activity);
                }

                PatientHistoryList.ItemsSource = PatientHistory;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� historii pacjenta.", "OK");
            }
        }

        private async void OnDeleteActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun�� t� czynno��?", "Tak", "Nie");
                if (!confirmDelete)
                {
                    Debug.WriteLine("Usuwanie anulowane przez u�ytkownika.");
                    return;
                }

                try
                {
                    bool success = await _databaseService.DeletePatientActionAsync(activity);

                    if (success)
                    {
                        PatientHistory.Remove(activity);
                        await DisplayAlert("Sukces", "Czynno�� zosta�a usuni�ta.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("B��d", "Nie uda�o si� usun�� czynno�ci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B��d podczas usuwania czynno�ci: {ex.Message}");
                    await DisplayAlert("B��d", "Wyst�pi� problem podczas usuwania czynno�ci.", "OK");
                }
            }
        }

        private async void OnEditActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                // Prompt user to change the action type
                string newActionType = await DisplayActionSheet(
                    "Zmie� typ akcji",
                    "Anuluj",
                    null,
                    "Dodanie wynik�w bada�", "Aktualizacja leczenia", "Zmiana danych pacjenta", "Podanie lek�w", "Dodanie komentarza"
                );

                if (!string.IsNullOrWhiteSpace(newActionType) && newActionType != "Anuluj")
                {
                    activity.ActionType = newActionType;
                }

                // Prompt user to update action details
                string newActionDetails = await DisplayPromptAsync(
                    "Edytuj szczeg�y czynno�ci",
                    "Zmie� szczeg�y czynno�ci:",
                    initialValue: activity.ActionDetails
                );

                if (!string.IsNullOrWhiteSpace(newActionDetails))
                {
                    activity.ActionDetails = newActionDetails;

                    try
                    {
                        bool success = await _databaseService.UpdatePatientActionAsync(activity);

                        if (success)
                        {
                            await DisplayAlert("Sukces", "Czynno�� zosta�a zaktualizowana.", "OK");
                            LoadPatientHistoryAsync(); // Refresh list
                        }
                        else
                        {
                            await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� czynno�ci.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B��d podczas aktualizacji czynno�ci: {ex.Message}");
                        await DisplayAlert("B��d", "Wyst�pi� problem podczas aktualizacji czynno�ci.", "OK");
                    }
                }
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void AddTapGestureToMainGrid()
        {
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnScreenTapped;
            MainGrid.GestureRecognizers.Add(tapGesture);
        }

        private void OnScreenTapped(object sender, EventArgs e)
        {
            ResetLogoutTimer();
        }

        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = 180;
            _logoutTimer?.Start();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _logoutTimer?.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logoutTimer?.Stop();
        }
    }
}
