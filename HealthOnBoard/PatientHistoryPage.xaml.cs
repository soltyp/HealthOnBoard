using HospitalManagementData;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;

namespace HealthOnBoard
{
    public partial class PatientHistoryPage : ContentPage
    {
        private readonly DashboardPage _dashboardPage;
        private readonly User _user;
        private readonly int _patientId;
        private readonly DatabaseService _databaseService;

        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<PatientActivity> PatientHistory { get; set; } = new ObservableCollection<PatientActivity>();

        public string SelectedActionType { get; set; }
        public string CurrentTemperature { get; set; }
        public bool IsTemperatureInputVisible { get; set; }
        public bool IsMedicationInputVisible { get; set; }

        private Medication _selectedMedication;
        public Medication SelectedMedication
        {
            get => _selectedMedication;
            set
            {
                _selectedMedication = value;
                OnPropertyChanged(nameof(SelectedMedication));
            }
        }

        private int _selectedQuantity = 1;
        public int SelectedQuantity
        {
            get => _selectedQuantity;
            set
            {
                _selectedQuantity = value;
                OnPropertyChanged(nameof(SelectedQuantity));
            }
        }

        private string _selectedUnit = "sztuka";
        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged(nameof(SelectedUnit));
            }
        }

        private System.Timers.Timer _logoutTimer;
        private const int LogoutTimeInSeconds = 180; // 3 minutes
        private int _remainingTimeInSeconds;

        public PatientHistoryPage(DashboardPage dashboardPage, User user, int patientId, DatabaseService databaseService)
        {
            InitializeComponent();
            _dashboardPage = dashboardPage;
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            BindingContext = this;

            UserFirstNameLabel.Text = user.FirstName ?? "Brak danych";
            RoleLabel.Text = GetRoleName(user.RoleID); // Use the role name retrieval method

            // Initialize the logout timer
            InitializeLogoutTimer();

            // Subscribe to history refresh messages
            MessagingCenter.Subscribe<EditActionPage>(this, "RefreshPatientActivityHistory", async (sender) =>
            {
                await RefreshPatientHistoryAsync();
            });

            // Load the patient's history
            LoadPatientHistoryAsync();
        }

        private string GetRoleName(int roleId)
        {
            try
            {
                // Fetch role from the database using the provided service
                var role = _databaseService.GetRoleById(roleId);
                return role?.RoleName ?? "Nieznana rola";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas pobierania nazwy roli: {ex.Message}");
                return "B³¹d roli";
            }
        }

        private async Task LoadPatientHistoryAsync()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam ³adowanie historii pacjenta...");

                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);
                var sortedActivities = activities.OrderByDescending(activity => activity.ActionDate);

                PatientHistory.Clear();
                foreach (var activity in sortedActivities)
                {
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê pobraæ historii pacjenta.", "OK");
            }
        }

        private async Task RefreshPatientHistoryAsync()
        {
            try
            {
                Debug.WriteLine("Odœwie¿anie historii pacjenta...");

                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);
                PatientHistory.Clear();
                foreach (var activity in activities.OrderByDescending(a => a.ActionDate))
                {
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas odœwie¿ania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê odœwie¿yæ historii pacjenta.", "OK");
            }
        }

        private void InitializeLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
            _logoutTimer = new System.Timers.Timer(1000);
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private void UpdateCountdown(object sender, ElapsedEventArgs e)
        {
            _remainingTimeInSeconds--;

            Dispatcher.Dispatch(() =>
            {
                int minutes = _remainingTimeInSeconds / 60;
                int seconds = _remainingTimeInSeconds % 60;
                LogoutTimerLabel.Text = $"{minutes:D2}:{seconds:D2}";

                if (_remainingTimeInSeconds <= 0)
                {
                    _logoutTimer.Stop();
                    LogoutUser();
                }
            });
        }
        private void OnScreenTapped(object sender, EventArgs e)
        {
            ResetLogoutTimer();
        }
        private async void LogoutUser()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
        }

        private async void OnEditActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                await Navigation.PushAsync(new EditActionPage(activity, _databaseService));
            }
        }

        private async void OnDeleteActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun¹æ tê czynnoœæ?", "Tak", "Nie");
                if (!confirmDelete) return;

                try
                {
                    bool success = await _databaseService.DeletePatientActionAsync(activity);
                    if (success)
                    {
                        PatientHistory.Remove(activity);
                        await DisplayAlert("Sukces", "Czynnoœæ zosta³a usuniêta.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ czynnoœci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B³¹d podczas usuwania czynnoœci: {ex.Message}");
                    await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas usuwania czynnoœci.", "OK");
                }
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz siê wylogowaæ?", "Tak", "Nie");
            if (confirmLogout)
            {
                await Navigation.PopToRootAsync();
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
