using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginService _loginService = new LoginService();

        public LoginPage()
        {
            InitializeComponent();
        }

        // Obsługa kliknięcia przycisku logowania
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var pin = PINEntry.Text;
            Debug.WriteLine($"PIN przed autoryzacją: {pin}");

            if (string.IsNullOrWhiteSpace(pin))
            {
                await DisplayAlert("Błąd", "Proszę wprowadzić PIN", "OK");
                return;
            }

            // Użycie LoginService do uwierzytelnienia użytkownika
            var user = await _loginService.AuthenticateUserAsync(pin);
            if (user != null)
            {
                Debug.WriteLine($"Uwierzytelniono użytkownika: {user.FirstName}");

                // Tworzenie nowej instancji User na podstawie pobranego użytkownika
                var userToPass = new User
                {
                    UserID = user.UserID,
                    FirstName = user.FirstName,
                    Role = user.Role,
                    ActiveStatus = user.ActiveStatus
                };

                Debug.WriteLine($"Przekazywany użytkownik(LoginPage.xaml.cs): FirstName = {userToPass.FirstName}, UserID = {userToPass.UserID}");

                // Przejdź do DashboardPage i przekaż nowy obiekt userToPass
                await Navigation.PushAsync(new DashboardPage(userToPass));

                await DisplayAlert("Sukces", $"Witaj, {userToPass.FirstName}!", "OK");
            }
            else
            {
                Debug.WriteLine("Błąd: Użytkownik nie został znaleziony lub uwierzytelnienie się nie powiodło.");
                await DisplayAlert("Błąd", "Niepoprawny PIN", "OK");
            }
        }
    }
}
