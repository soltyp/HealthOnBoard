using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientDetailsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly int _patientId;
        private readonly User _user; // User information
        private System.Timers.Timer _logoutTimer;
        private const int LogoutTimeInSeconds = 180; // 3 minutes
        private int _remainingTimeInSeconds;

        public PatientDetailsPage(User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();

            // Assign parameters to fields
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Update UI and initialize components
            UpdateNavbar();
            InitializeLogoutTimer();
            AddTapGestureToResetTimer();

            // Load patient details
            LoadPatientDetailsAsync();
        }

        // Update navbar information
        private void UpdateNavbar()
        {
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = _user.Role ?? "Brak roli";
        }

        // Add tap gesture to reset the timer
        private void AddTapGestureToResetTimer()
        {
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (_, _) => ResetLogoutTimer();
            MainGrid.GestureRecognizers.Add(tapGesture);
        }

        // Async method to load patient details
        private async void LoadPatientDetailsAsync()
        {
            try
            {
                var patient = await _databaseService.GetPatientDetailsAsync(_patientId);

                if (patient != null)
                {
                    // Update UI with patient details
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


        // Initialize the logout timer
        private void InitializeLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
            _logoutTimer = new System.Timers.Timer(1000); // 1 second interval
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        // Update the countdown for the logout timer
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

        // Handle logout when timer expires
        private async void LogoutUser()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        // Reset the logout timer
        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
        }

        // Handle back button click
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }



    }
}
