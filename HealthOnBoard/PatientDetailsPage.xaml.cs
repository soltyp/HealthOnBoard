using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientDetailsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly int _patientId;
        private readonly User _user; // Informacje o u¿ytkowniku
        private System.Timers.Timer _logoutTimer;
        private const int LogoutTimeInSeconds = 180; // 3 minuty
        private int _remainingTimeInSeconds;

        public PatientDetailsPage(User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId > 0 ? patientId : throw new ArgumentException("Invalid Patient ID", nameof(patientId));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "DatabaseService cannot be null");

            // Ustaw dane u¿ytkownika
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = GetRoleName(_user.RoleID) ?? "Brak roli";

            // Inicjalizacja timera
            InitializeLogoutTimer();

            // Dodanie obs³ugi dotkniêcia ekranu
            AddTapGestureToMainGrid();

            // Za³aduj dane pacjenta
            LoadPatientDetailsAsync();

            BindingContext = this;
        }

        // Pobierz nazwê roli na podstawie RoleID
        private string GetRoleName(int roleId)
        {
            try
            {
                var role = _databaseService.GetRoleById(roleId);
                return role?.RoleName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas pobierania nazwy roli: {ex.Message}");
                return null;
            }
        }

        // Dodanie gestu do resetowania timera
        private void AddTapGestureToMainGrid()
        {
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (_, _) => ResetLogoutTimer();
            MainGrid.GestureRecognizers.Add(tapGesture);
        }

        // Asynchroniczne za³adowanie danych pacjenta
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
                    PatientBloodTypeLabel.Text = patient.BloodType?.Type ?? "Brak danych";
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
                Debug.WriteLine($"B³¹d podczas ³adowania danych pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas ³adowania danych pacjenta.", "OK");
            }
        }

        // Inicjalizacja timera wylogowania
        private void InitializeLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
            _logoutTimer = new System.Timers.Timer(1000); // 1 sekunda
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        // Aktualizacja timera wylogowania
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

        // Wylogowanie po wygaœniêciu czasu
        private async void LogoutUser()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        // Resetowanie timera
        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
        }

        // Obs³uga przycisku powrotu
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
