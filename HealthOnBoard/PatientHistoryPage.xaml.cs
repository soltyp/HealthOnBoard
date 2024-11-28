using System.Diagnostics;
using System.Timers;

namespace HealthOnBoard
{
    public partial class PatientHistoryPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly int _patientId;
        private readonly User _user;
        private System.Timers.Timer _logoutTimer;
        private int _remainingTimeInSeconds = 180; // 3 minuty (180 sekund)

        public PatientHistoryPage(User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "DatabaseService cannot be null");

            // Ustaw dane u¿ytkownika w navbarze
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = _user.Role ?? "Brak danych";

            // Inicjalizacja timera
            InitializeLogoutTimer();

            // Dodaj gest dotkniêcia do g³ównego kontenera
            AddTapGestureToMainGrid();

            // Za³aduj historiê leczenia
            LoadPatientHistoryAsync();
        }

        private void InitializeLogoutTimer()
        {
            _logoutTimer = new System.Timers.Timer(1000); // 1 sekunda
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
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        private async void LoadPatientHistoryAsync()
        {
            try
            {
                var history = await _databaseService.GetRecentActivitiesAsync(_patientId, limit: int.MaxValue);
                PatientHistoryList.ItemsSource = history;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ historii pacjenta.", "OK");
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
            _remainingTimeInSeconds = 180; // Reset czasu do 3 minut
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
