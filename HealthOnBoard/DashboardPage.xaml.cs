using Microsoft.Maui.Controls;
using System.Diagnostics;
using HospitalManagementAPI.Models;

namespace HealthOnBoard
{
    public partial class DashboardPage : ContentPage
    {
        private readonly User _user;
        private readonly Patient _patient;

        public DashboardPage(User user, Patient patient)
        {
            InitializeComponent();
            _user = user;
            _patient = patient;

            // Logowanie do konsoli dla cel�w debugowania
            Debug.WriteLine(_user != null ? $"DashboardPage: FirstName = {_user.FirstName}, UserID = {_user.UserID}" : "DashboardPage: u�ytkownik jest null");
            Debug.WriteLine(_patient != null ? $"DashboardPage: PatientName = {_patient.Name}, BedNumber = {_patient.BedNumber}" : "DashboardPage: pacjent jest null");

            // Ustawienie warto�ci UI na podstawie obiektu u�ytkownika
            if (_user != null)
            {
                WelcomeLabel.Text = $"Witaj, {_user.FirstName}!";
                RoleLabel.Text = $"Rola: {_user.Role}";
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

            // Ustawienie warto�ci UI na podstawie obiektu pacjenta
            if (_patient != null)
            {
                PatientNameLabel.Text = _patient.Name;
                PatientAgeLabel.Text = _patient.Age.ToString();
                BedNumberLabel.Text = _patient.BedNumber.ToString();
            }
            else
            {
                PatientNameLabel.Text = "Brak danych";
                PatientAgeLabel.Text = "-";
                BedNumberLabel.Text = "-";
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
