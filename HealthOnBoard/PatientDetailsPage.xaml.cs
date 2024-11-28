using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientDetailsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly int _patientId;
        private readonly User _user; // Dodano obiekt User
        private System.Timers.Timer _logoutTimer;
        private int _remainingTimeInSeconds = 180; // 3 minuty (180 sekund)

        public PatientDetailsPage(User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Zaktualizuj navbar na podstawie danych u¿ytkownika
            UpdateNavbar();

            // Inicjalizacja timera
            InitializeLogoutTimer();

            // Dodanie gestu resetowania timera
            AddTapGestureToMainGrid();

            // £adowanie danych pacjenta
            LoadPatientDetailsAsync();
        }

        private void UpdateNavbar()
        {
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = _user.Role ?? "Brak roli";
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

        private async void LoadPatientDetailsAsync()
        {
            try
            {
                var patient = await _databaseService.GetPatientDetailsAsync(_patientId);

                if (patient != null)
                {
                    PatientNameLabel.Text = patient.Name ?? "Brak danych";
                    PatientAgeLabel.Text = $"{patient.Age} lat";
                    PatientPESELLabel.Text = patient.PESEL ?? "Brak danych";
                    PatientAddressLabel.Text = patient.Address ?? "Brak danych";
                    PatientPhoneNumberLabel.Text = patient.PhoneNumber ?? "Brak danych";
                    PatientEmailLabel.Text = patient.Email ?? "Brak danych";
                    PatientDateOfBirthLabel.Text = patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "Brak danych";
                    PatientGenderLabel.Text = patient.Gender ?? "Brak danych";
                    PatientEmergencyContactLabel.Text = patient.EmergencyContact ?? "Brak danych";
                    PatientBloodTypeLabel.Text = patient.BloodType ?? "Brak danych";
                    PatientAllergiesLabel.Text = patient.Allergies ?? "Brak danych";
                    PatientChronicDiseasesLabel.Text = patient.ChronicDiseases ?? "Brak danych";
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie znaleziono danych pacjenta.", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania szczegó³ów pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas ³adowania danych pacjenta.", "OK");
            }
        }

        private void InitializeLogoutTimer()
        {
            _logoutTimer = new System.Timers.Timer(1000); // 1 sekunda
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private void UpdateCountdown(object sender, System.Timers.ElapsedEventArgs e)
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

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = 180; // Resetuje czas do 3 minut
            _logoutTimer?.Start();
        }
    }
}
