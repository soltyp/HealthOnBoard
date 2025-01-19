using HospitalManagementData;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;

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

            // Nas�uchiwanie na wiadomo�� o aktualizacji historii
            MessagingCenter.Subscribe<EditActionPage>(this, "RefreshPatientActivityHistory", async (sender) =>
            {
                await RefreshPatientHistoryAsync();
            });

            InitializeLogoutTimer();
            LoadPatientHistoryAsync();
        }

        private async Task LoadPatientHistoryAsync()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam �adowanie historii pacjenta...");

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
                Debug.WriteLine($"B��d podczas �adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� pobra� historii pacjenta.", "OK");
            }
        }

        private async Task RefreshPatientHistoryAsync()
        {
            try
            {
                Debug.WriteLine("Od�wie�anie historii pacjenta...");

                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);
                PatientHistory.Clear();
                foreach (var activity in activities.OrderByDescending(a => a.ActionDate))
                {
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas od�wie�ania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� od�wie�y� historii pacjenta.", "OK");
            }
        }

        private void UpdateViewForSelectedAction()
        {
            IsTemperatureInputVisible = SelectedActionType == "Pomiar temperatury";
            IsMedicationInputVisible = SelectedActionType == "Podanie lek�w";

            OnPropertyChanged(nameof(IsTemperatureInputVisible));
            OnPropertyChanged(nameof(IsMedicationInputVisible));
        }

        private void InitializeLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
            _logoutTimer = new System.Timers.Timer(1000);
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private void UpdateCountdown(object sender, System.Timers.ElapsedEventArgs e)
        {
            _remainingTimeInSeconds--;
            Dispatcher.Dispatch(() =>
            {
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
                await DisplayAlert("Sesja wygas�a", "Twoja sesja wygas�a z powodu braku aktywno�ci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        private void OnScreenTapped(object sender, EventArgs e)
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
                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun�� t� czynno��?", "Tak", "Nie");
                if (!confirmDelete) return;

                try
                {
                    bool success = await _databaseService.DeletePatientActionAsync(activity);
                    if (success)
                    {
                        PatientHistory.Remove(activity);
                        await DisplayAlert("Sukces", "Czynno�� zosta�a usuni�ta.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("B��d", "Nie uda�o si� usun�� czynno�ci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B��d podczas usuwania czynno�ci: {ex.Message}");
                    await DisplayAlert("B��d", "Wyst�pi� problem podczas usuwania czynno�ci.", "OK");
                }
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
