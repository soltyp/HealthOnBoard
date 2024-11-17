using Microsoft.Maui.Controls;
using System.Diagnostics;
using HospitalManagementAPI.Models;
using System.Timers;

namespace HealthOnBoard
{
    public partial class DashboardPage : ContentPage
    {
        private readonly User _user;
        private readonly Patient _patient;

        private System.Timers.Timer _logoutTimer;
        private int _remainingTimeInSeconds = 180; // 3 minuty

        public DashboardPage(User user, Patient patient)
        {
            InitializeComponent();
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patient = patient ?? throw new ArgumentNullException(nameof(patient), "Patient cannot be null");

            // Ustawienia UI
            WelcomeLabel.Text = $"Witaj, {_user.FirstName}!";
            RoleLabel.Text = $"{_user.Role}";
            UserIDLabel.Text = $"{_user.UserID}";
            ActiveStatusLabel.Text = _user.ActiveStatus ? "Aktywny" : "Nieaktywny";

            PatientNameLabel.Text = _patient.Name ?? "Brak danych";
            PatientAgeLabel.Text = _patient.Age > 0 ? _patient.Age.ToString() : "Brak danych";
            BedNumberLabel.Text = _patient.BedNumber > 0 ? _patient.BedNumber.ToString() : "Brak danych";

            // Inicjalizacja timera
            InitializeLogoutTimer();

            // Obs³uga dotkniêcia pustych miejsc na ekranie
            AddTapGestureToMainGrid();
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

        private void InitializeLogoutTimer()
        {
            // Ustaw timer na 1 sekundê dla odliczania
            _logoutTimer = new System.Timers.Timer(1000); // 1 sekunda
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private async void UpdateCountdown(object sender, ElapsedEventArgs e)
        {
            // Zmniejsz pozosta³y czas
            _remainingTimeInSeconds--;

            // Aktualizuj UI w w¹tku g³ównym
            await Dispatcher.DispatchAsync(() =>
            {
                int minutes = _remainingTimeInSeconds / 60;
                int seconds = _remainingTimeInSeconds % 60;

                CountdownLabel.Text = $"Pozosta³y czas do wylogowania: {minutes:D2}:{seconds:D2}";

                // Wyloguj, gdy czas siê skoñczy
                if (_remainingTimeInSeconds <= 0)
                {
                    _logoutTimer.Stop();
                    LogoutUser();
                }
            });
        }

        private async void LogoutUser()
        {
            await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
            await Navigation.PopToRootAsync();
        }

        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = 180; // Reset czasu do 3 minut
            _logoutTimer?.Start();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ResetLogoutTimer();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logoutTimer?.Stop();
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            _logoutTimer?.Stop();
            bool confirmLogout = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz siê wylogowaæ?", "Tak", "Nie");
            if (confirmLogout)
            {
                await Navigation.PopToRootAsync();
            }
            else
            {
                ResetLogoutTimer();
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            ResetLogoutTimer();
            await DisplayAlert("Ustawienia", "Przejœcie do ustawieñ u¿ytkownika.", "OK");
        }
    }
}
