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

            // Logowanie do konsoli dla cel�w debugowania
            Debug.WriteLine(_user != null ? $"DashboardPage: FirstName = {_user.FirstName}, UserID = {_user.UserID}" : "DashboardPage: u�ytkownik jest null");

            // Ustawienie warto�ci UI na podstawie obiektu u�ytkownika
            if (_user != null)
            {
                WelcomeLabel.Text = $"Witaj, {_user.FirstName}!";
                RoleLabel.Text = $"Rola: {_user.Role}"; // Mo�na doda� mapowanie na pe�n� nazw� roli, np. "Administrator"
                UserIDLabel.Text = $"{_user.UserID}";
                ActiveStatusLabel.Text = _user.ActiveStatus ? "Aktywny" : "Nieaktywny";
            }
            else
            {
                WelcomeLabel.Text = "B��d: u�ytkownik jest null!";
                RoleLabel.Text = "Brak danych";
                UserIDLabel.Text = "-";
                ActiveStatusLabel.Text = "-";
            }
        }

        // Metoda obs�uguj�ca klikni�cie przycisku "Wyloguj si�"
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz si� wylogowa�?", "Tak", "Nie");
            if (confirmLogout)
            {
                // Logika wylogowania (np. nawigacja do strony logowania)
                await Navigation.PopToRootAsync();
            }
        }

        // Metoda obs�uguj�ca klikni�cie przycisku "Ustawienia"
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            // Przyk�adowa logika do przej�cia do strony ustawie� (je�li istnieje)
            await DisplayAlert("Ustawienia", "Przej�cie do ustawie� u�ytkownika.", "OK");
        }
    }
}
