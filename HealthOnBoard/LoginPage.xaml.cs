using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using HospitalManagementAPI;
using HospitalManagementAPI.Models;
using System.Collections.Generic;
using System.Linq;

namespace HealthOnBoard
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginService _loginService;
        private readonly DatabaseService _databaseService;

        public bool IsPinInputVisible { get; set; } = true;
        public bool IsSecurityPinInputVisible { get; set; } = false;

        // Lista numerów łóżek
        public List<int> BedNumbers { get; } = Enumerable.Range(1, 5).ToList();

        private int _selectedBedNumber = 1; // Domyślnie wybrany numer łóżka
        public int SelectedBedNumber
        {
            get => _selectedBedNumber;
            set
            {
                if (_selectedBedNumber != value)
                {
                    _selectedBedNumber = value;
                    Debug.WriteLine($"Wybrano numer łóżka: {_selectedBedNumber}");
                    OnPropertyChanged(nameof(SelectedBedNumber));
                }
            }
        }

        public LoginPage(IConfiguration configuration)
        {
            InitializeComponent();
            _loginService = new LoginService();
            _databaseService = new DatabaseService(configuration);
            BindingContext = this; // Powiązanie z kontekstem danych
        }

        // Obsługa kliknięcia przycisku logowania
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var pin = PINEntry.Text;
            Debug.WriteLine($"PIN przed autoryzacją: {pin}");
            Debug.WriteLine($"Wybrany numer łóżka: {SelectedBedNumber}");

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

                // Pobranie pacjenta przypisanego do wybranego numeru łóżka
                var patient = await _databaseService.GetPatientByBedNumberAsync(SelectedBedNumber);

                if (patient != null)
                {
                    // Przejście do DashboardPage z danymi użytkownika i pacjenta
                    await Navigation.PushAsync(new DashboardPage(user, patient));
                    await DisplayAlert("Sukces", $"Witaj, {user.FirstName}! Pacjent: {patient.Name}, Łóżko: {SelectedBedNumber}", "OK");
                }
                else
                {
                    await DisplayAlert("Uwaga", $"Brak pacjenta przypisanego do łóżka {SelectedBedNumber}.", "OK");
                }
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
