using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using HospitalManagementAPI;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace HealthOnBoard
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginService _loginService;
        private readonly DatabaseService _databaseService;
        private readonly BedService _bedService;
        private readonly IConfiguration _configuration;

        public List<int> BedNumbers { get; set; } = new List<int>();

       

        public bool IsPinInputVisible { get; set; } = true;
        public bool IsSecurityPinInputVisible { get; set; } = false;

        // Lista numerów łóżek
        
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
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Wymuś ustawienie domyślnego numeru łóżka, jeśli picker nie działa poprawnie
            if (BedNumbers != null && BedNumbers.Contains(1))
            {
                SelectedBedNumber = 1;
                OnPropertyChanged(nameof(SelectedBedNumber));
            }
        }

        // Zmienne blokady
        private bool _isLockedOut;
        private DateTime _lockoutEndTime;
        private int _failedAttempts; // Licznik nieudanych prób
        private System.Timers.Timer _lockoutTimer;

        public LoginPage(IConfiguration configuration)
        {
            InitializeComponent();
            _loginService = new LoginService(); // Inicjalizacja usługi logowania
            _databaseService = new DatabaseService(configuration);
            _bedService = new BedService(_databaseService);
            BindingContext = this; // Powiązanie z kontekstem danych
            LoadBedsAsync();
        }

        private async void LoadBedsAsync()
        {
            try
            {
                var beds = await _bedService.GetAllBedsAsync();
                BedNumbers = beds.Select(b => b.BedNumber).ToList();

                // Dodaj numer "1" do listy, jeśli go nie ma
                if (!BedNumbers.Contains(1))
                {
                    BedNumbers.Insert(0, 1);
                }

                // Ustaw domyślny numer łóżka na "1"
                SelectedBedNumber = 1;

                // Powiadom o zmianach w liście i wyborze
                OnPropertyChanged(nameof(BedNumbers));
                OnPropertyChanged(nameof(SelectedBedNumber));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas ładowania łóżek: {ex.Message}");
                await DisplayAlert("Błąd", "Nie udało się załadować listy łóżek.", "OK");
            }
        }



        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (_isLockedOut)
            {
                await DisplayAlert("Blokada aktywna", "System jest zablokowany. Użyj PIN-u bezpieczeństwa lub poczekaj na zakończenie blokady.", "OK");
                return;
            }

            var pin = PINEntry.Text;
            var selectedBedNumber = SelectedBedNumber; // Pobierz wybrany numer łóżka
            Debug.WriteLine($"PIN przed autoryzacją: {pin}, Łóżko: {selectedBedNumber}");

            if (string.IsNullOrWhiteSpace(pin))
            {
                await DisplayAlert("Błąd", "Proszę wprowadzić PIN", "OK");
                return;
            }

            var user = await _loginService.AuthenticateUserAsync(PINEntry.Text, SecurityPinEntry.Text, selectedBedNumber);
            if (user != null)
            {
                Debug.WriteLine($"Uwierzytelniono użytkownika: {user.FirstName}, Łóżko: {selectedBedNumber}");

                if (_loginService.IsAdmin(user))
                {
                    await Navigation.PushAsync(new AdminPanelPage(_databaseService));
                }
                else
                {
                    await NavigateToDashboard(user);
                }

                _failedAttempts = 0; // Reset liczby nieudanych prób po sukcesie
            }
            else
            {
                _failedAttempts++;
                if (_failedAttempts >= 3)
                {
                    StartLockout();
                    await DisplayAlert("Blokada", "Logowanie zablokowane po 3 nieudanych próbach. Użyj PIN-u bezpieczeństwa lub poczekaj 3 minuty.", "OK");
                }
                else
                {
                    await DisplayAlert("Błąd", $"Niepoprawny PIN. Pozostało prób: {3 - _failedAttempts}.", "OK");
                }
            }
        }

        private void StartLockout()
        {
            _isLockedOut = true;
            _lockoutEndTime = DateTime.Now.AddMinutes(3);
            LockoutMessage.IsVisible = true;
            LockoutMessage.Text = "Logowanie zablokowane. Spróbuj ponownie za 180 sekund.";

            IsPinInputVisible = false;
            IsSecurityPinInputVisible = true;
            OnPropertyChanged(nameof(IsPinInputVisible));
            OnPropertyChanged(nameof(IsSecurityPinInputVisible));

            _lockoutTimer = new System.Timers.Timer(1000); // Timer aktualizuje co sekundę
            _lockoutTimer.Elapsed += UpdateLockoutMessage;
            _lockoutTimer.Start();
        }

        private void UpdateLockoutMessage(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Dispatch(() =>
            {
                var remainingTime = _lockoutEndTime - DateTime.Now;
                if (remainingTime.TotalSeconds <= 0)
                {
                    EndLockout();
                }
                else
                {
                    LockoutMessage.Text = $"Logowanie zablokowane. Spróbuj ponownie za {Math.Ceiling(remainingTime.TotalSeconds)} sekund.";
                }
            });
        }

        private void EndLockout()
        {
            _isLockedOut = false;
            _failedAttempts = 0; // Reset liczby nieudanych prób
            LockoutMessage.IsVisible = false;
            _lockoutTimer?.Stop();
            _lockoutTimer?.Dispose();

            IsPinInputVisible = true;
            IsSecurityPinInputVisible = false;
            OnPropertyChanged(nameof(IsPinInputVisible));
            OnPropertyChanged(nameof(IsSecurityPinInputVisible));
        }

        private async Task NavigateToDashboard(User user)
        {
            // Pobierz pacjenta przypisanego do wybranego numeru łóżka
            var patient = await _databaseService.GetPatientByBedNumberAsync(SelectedBedNumber);

            if (patient != null)
            {
                // Przekazujemy User, Patient oraz DatabaseService do DashboardPage
                await Navigation.PushAsync(new DashboardPage(user, patient, _databaseService));
                await DisplayAlert("Sukces", $"Witaj, {user.FirstName}! Pacjent: {patient.Name}, Łóżko: {SelectedBedNumber}", "OK");
            }
            else
            {
                await DisplayAlert("Uwaga", $"Brak pacjenta przypisanego do łóżka {SelectedBedNumber}.", "OK");
            }
        }


        private async void OnSecurityPinUnlockClicked(object sender, EventArgs e)
        {
            var securityPin = SecurityPinEntry.Text;

            if (string.IsNullOrWhiteSpace(securityPin))
            {
                await DisplayAlert("Błąd", "Proszę wprowadzić PIN bezpieczeństwa", "OK");
                return;
            }

            var success = await _loginService.AuthenticateSecurityPinAsync(securityPin);
            if (success)
            {
                await DisplayAlert("Sukces", "Odblokowano możliwość logowania", "OK");
                EndLockout(); // Natychmiastowe zakończenie blokady
            }
            else
            {
                await DisplayAlert("Błąd", "Niepoprawny PIN bezpieczeństwa", "OK");
            }
        }

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
