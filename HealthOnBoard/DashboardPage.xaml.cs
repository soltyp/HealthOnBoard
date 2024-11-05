using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class DashboardPage : ContentPage
    {
        private readonly User? _user;

        public DashboardPage(User user)
        {
            InitializeComponent();
            _user = user;

            // Logowanie do konsoli dla celów debugowania
            Debug.WriteLine(_user != null ? $"DashboardPage: FirstName = {_user.FirstName}, UserID = {_user.UserID}" : "DashboardPage: u¿ytkownik jest null");

            // Ustawienie wartoœci UI na podstawie obiektu u¿ytkownika
            if (_user != null)
            {
                WelcomeLabel.Text = $"Witaj, {_user.FirstName}!";
                RoleLabel.Text = $"Rola: {_user.Role}"; // Mo¿na dodaæ mapowanie na pe³n¹ nazwê roli, np. "Administrator"
                UserIDLabel.Text = $"{_user.UserID}";
                ActiveStatusLabel.Text = _user.ActiveStatus ? "Aktywny" : "Nieaktywny";
            }
            else
            {
                WelcomeLabel.Text = "B³¹d: u¿ytkownik jest null!";
                RoleLabel.Text = "Brak danych";
                UserIDLabel.Text = "-";
                ActiveStatusLabel.Text = "-";
            }
        }

        // Metoda obs³uguj¹ca klikniêcie przycisku "Wyloguj siê"
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz siê wylogowaæ?", "Tak", "Nie");
            if (confirmLogout)
            {
                // Logika wylogowania (np. nawigacja do strony logowania)
                await Navigation.PopToRootAsync();
            }
        }

        // Metoda obs³uguj¹ca klikniêcie przycisku "Ustawienia"
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            // Przyk³adowa logika do przejœcia do strony ustawieñ (jeœli istnieje)
            await DisplayAlert("Ustawienia", "Przejœcie do ustawieñ u¿ytkownika.", "OK");
        }
    }
}
