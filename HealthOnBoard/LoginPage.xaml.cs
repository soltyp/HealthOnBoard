using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using HospitalManagementAPI;
using HospitalManagementAPI.Models;

namespace HealthOnBoard
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginService _loginService;
        private readonly DatabaseService _databaseService;

        public bool IsPinInputVisible { get; set; } = true;
        public bool IsSecurityPinInputVisible { get; set; } = false;

        public LoginPage(IConfiguration configuration)
        {
            InitializeComponent();
            _loginService = new LoginService();
            _databaseService = new DatabaseService(configuration);
            BindingContext = this;
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

            var isLockedOut = await _databaseService.IsLockedOutAsync();
            if (isLockedOut)
            {
                await DisplayAlert("Uwaga", "Logowanie zablokowane po 3 nieudanych próbach. Wprowadź PIN bezpieczeństwa, aby odblokować.", "OK");
                IsPinInputVisible = false;
                IsSecurityPinInputVisible = true;
                OnPropertyChanged(nameof(IsPinInputVisible));
                OnPropertyChanged(nameof(IsSecurityPinInputVisible));
                return;
            }

            var user = await _loginService.AuthenticateUserAsync(pin);
            if (user != null)
            {
                Debug.WriteLine($"Uwierzytelniono użytkownika: {user.FirstName}");
                await Navigation.PushAsync(new DashboardPage(user));
                await DisplayAlert("Sukces", $"Witaj, {user.FirstName}!", "OK");
            }
            else
            {
                await DisplayAlert("Błąd", "Niepoprawny PIN", "OK");
            }
        }

        // Obsługa kliknięcia przycisku logowania PIN bezpieczeństwa
        private async void OnSecurityPinUnlockClicked(object sender, EventArgs e)
        {
            var securityPin = SecurityPinEntry.Text;

            if (string.IsNullOrWhiteSpace(securityPin))
            {
                await DisplayAlert("Błąd", "Proszę wprowadzić PIN bezpieczeństwa", "OK");
                return;
            }

            var success = await _databaseService.AuthenticateSecurityPinAsync(securityPin);
            if (success)
            {
                await DisplayAlert("Sukces", "Odblokowano możliwość logowania", "OK");
                IsPinInputVisible = true;
                IsSecurityPinInputVisible = false;
                OnPropertyChanged(nameof(IsPinInputVisible));
                OnPropertyChanged(nameof(IsSecurityPinInputVisible));
            }
            else
            {
                await DisplayAlert("Błąd", "Niepoprawny PIN bezpieczeństwa", "OK");
            }
        }

        // Obsługa kliknięcia przycisku klawiatury numerycznej
        private void OnNumberClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                var number = button.Text;

                if (IsPinInputVisible)
                {
                    PINEntry.Text += number;
                }
                else if (IsSecurityPinInputVisible)
                {
                    SecurityPinEntry.Text += number;
                }
            }
        }


        // Obsługa kliknięcia przycisku Cofnij
        private void OnBackspaceClicked(object sender, EventArgs e)
        {
            if (IsPinInputVisible && !string.IsNullOrEmpty(PINEntry.Text))
            {
                PINEntry.Text = PINEntry.Text[..^1];
            }
            else if (IsSecurityPinInputVisible && !string.IsNullOrEmpty(SecurityPinEntry.Text))
            {
                SecurityPinEntry.Text = SecurityPinEntry.Text[..^1];
            }
        }

        // Obsługa kliknięcia przycisku OK
        private async void OnOkClicked(object sender, EventArgs e)
        {
            if (IsPinInputVisible)
            {
                OnLoginClicked(sender, e);
            }
            else if (IsSecurityPinInputVisible)
            {
                OnSecurityPinUnlockClicked(sender, e);
            }
        }
    }
}
