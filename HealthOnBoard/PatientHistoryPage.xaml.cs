using HospitalManagementData;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientHistoryPage : ContentPage
    {
        // Lista dost�pnych lek�w
        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();

        // Wybrany lek
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

        // Temperatura do wprowadzenia
        private string _currentTemperature;
        public string CurrentTemperature
        {
            get => _currentTemperature;
            set
            {
                _currentTemperature = value;
                OnPropertyChanged(nameof(CurrentTemperature));
            }
        }

        // Dodaj pola dla userId i patientId, je�li ich brakuje
        private readonly int userId;
        private readonly int patientId;

        private readonly User _user;
        private readonly int _patientId;
        private readonly DatabaseService _databaseService;
        public ObservableCollection<PatientActivity> PatientHistory { get; set; } = new ObservableCollection<PatientActivity>();
        private System.Timers.Timer _logoutTimer;
        private const int LogoutTimeInSeconds = 180; // 3 minutes
        private int _remainingTimeInSeconds;


        private readonly DashboardPage _dashboardPage;
        private string _selectedActionType;
        public string SelectedActionType
        {
            get => _selectedActionType;
            set
            {
                _selectedActionType = value;
                OnPropertyChanged(nameof(SelectedActionType));
                UpdateViewForSelectedAction();
            }
        }
        public bool IsTemperatureInputVisible { get; set; }
        public bool IsMedicationInputVisible { get; set; }


        public PatientHistoryPage(DashboardPage dashboardPage, User user, int patientId, DatabaseService databaseService)
{
    InitializeComponent();
            _dashboardPage = dashboardPage; // Przechowujemy instancj� DashboardPage
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patientId = patientId;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            InitializeLogoutTimer();
            LoadPatientHistoryAsync();
            BindingContext = this;
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");

        }

        private async Task LoadPatientHistoryAsync()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam �adowanie historii pacjenta...");

                // Pobierz dane z bazy danych
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);

                // Posortuj dane wed�ug ActionDate malej�co
                var sortedActivities = activities.OrderByDescending(activity => activity.ActionDate);

                // Wyczy�� istniej�c� list� i za�aduj nowe dane
                PatientHistory.Clear();
                foreach (var activity in sortedActivities)
                {
                    if (activity.LogID == 0)
                    {
                        Debug.WriteLine($"B��d: LogID jest r�wny 0 dla czynno�ci: {activity.ActionType}");
                        continue;
                    }
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� pobra� historii pacjenta.", "OK");
            }
        }



        private void UpdateViewForSelectedAction()
        {
            switch (SelectedActionType)
            {
                case "Pomiar temperatury":
                    IsTemperatureInputVisible = true;
                    IsMedicationInputVisible = false;
                    break;
                case "Podanie lek�w":
                    IsMedicationInputVisible = true;
                    IsTemperatureInputVisible = false;
                    break;
                default:
                    IsTemperatureInputVisible = false;
                    IsMedicationInputVisible = false;
                    break;
            }

            OnPropertyChanged(nameof(IsTemperatureInputVisible));
            OnPropertyChanged(nameof(IsMedicationInputVisible));
        }

        private void LoadActivityDetails(PatientActivity activity)
        {
            SelectedActionType = activity.ActionType;

            switch (activity.ActionType)
            {
                case "Pomiar temperatury":
                    CurrentTemperature = activity.CurrentTemperature?.ToString("F1") ?? "";
                    break;
                case "Podanie lek�w":
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name == activity.ActionDetails);
                    break;
                    // Dodaj inne przypadki w zale�no�ci od potrzeb
            }
        }

        private async Task SaveActivityAsync()
        {
            var newActivity = new PatientActivity
            {
                ActionType = SelectedActionType,
                ActionDetails = SelectedActionType == "Podanie lek�w" ? SelectedMedication.Name : null,
                CurrentTemperature = SelectedActionType == "Pomiar temperatury" ? decimal.Parse(CurrentTemperature) : null,
                ActionDate = DateTime.Now
            };

            // Zapisz do bazy danych
            var success = await _databaseService.AddPatientActionAsync(userId, patientId, newActivity.ActionType, newActivity.ActionDetails, newActivity.ActionDate);
            if (success)
            {
                await DisplayAlert("Sukces", "Akcja zosta�a zapisana.", "OK");
            }
            else
            {
                await DisplayAlert("B��d", "Nie uda�o si� zapisa� akcji.", "OK");
            }
        }
        private async void OnEditActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                Debug.WriteLine($"Editing Action - LogID: {activity.LogID}, PatientID: {activity.PatientID}, ActionType: {activity.ActionType}");
                await Navigation.PushAsync(new EditActionPage(activity, _databaseService));
            }
            else
            {
                Debug.WriteLine("Invalid CommandParameter or sender.");
                await DisplayAlert("B��d", "Nie uda�o si� pobra� danych do edycji.", "OK");
            }
        }





        //private async void OnEditActionClicked(object sender, EventArgs e)
        //{
        //    if (sender is Button button && button.CommandParameter is PatientActivity activity)
        //    {
        //        // Obs�uga edycji na podstawie typu akcji
        //        string newValue = await DisplayPromptAsync(
        //            $"Edytuj {activity.ActionType}",
        //            $"Wprowad� now� warto�� dla: {activity.ActionDetails}",
        //            initialValue: activity.ActionDetails
        //        );

        //        if (!string.IsNullOrEmpty(newValue))
        //        {
        //            try
        //            {
        //                activity.ActionDetails = newValue; // Zaktualizuj lokaln� warto��
        //                bool success = await _databaseService.UpdateActivityDetailsAsync(activity);

        //                if (success)
        //                {
        //                    await DisplayAlert("Sukces", "Dane zosta�y zaktualizowane.", "OK");
        //                    await RefreshPatientHistoryAsync();
        //                }
        //                else
        //                {
        //                    await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� danych w bazie.", "OK");
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"B��d podczas aktualizacji danych: {ex.Message}");
        //                await DisplayAlert("B��d", "Wyst�pi� problem podczas aktualizacji danych.", "OK");
        //            }
        //        }
        //        else
        //        {
        //            await DisplayAlert("Anulowano", "Edycja zosta�a anulowana.", "OK");
        //        }
        //    }
        //}







        private async Task RefreshPatientHistoryAsync()
        {
            try
            {
                // Pobierz zaktualizowan� histori� pacjenta
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);

                // Wyczy�� bie��c� list� i za�aduj nowe dane
                PatientHistory.Clear();
                foreach (var activity in activities)
                {
                    PatientHistory.Add(activity);
                }

                Debug.WriteLine("Historia pacjenta zosta�a od�wie�ona.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas od�wie�ania historii pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� od�wie�y� historii pacjenta.", "OK");
            }
        }


       



        private async void OnDeleteActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                Debug.WriteLine($"Rozpoczynam usuwanie czynno�ci: {activity.ActionType}, {activity.ActionDetails}, {activity.ActionDate}, PatientID: {activity.PatientID}");

                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun�� t� czynno��?", "Tak", "Nie");
                if (!confirmDelete)
                {
                    Debug.WriteLine("Usuwanie anulowane przez u�ytkownika.");
                    return;
                }

                try
                {
                    Debug.WriteLine("Wywo�ywanie metody DeletePatientActionAsync...");
                    bool success = await _databaseService.DeletePatientActionAsync(activity);

                    if (success)
                    {
                        Debug.WriteLine("Czynno�� zosta�a usuni�ta z bazy danych.");
                        Debug.WriteLine($"Przed usuni�ciem: Liczba element�w w PatientHistory: {PatientHistory.Count}");
                        PatientHistory.Remove(activity);
                        Debug.WriteLine($"Po usuni�ciu: Liczba element�w w PatientHistory: {PatientHistory.Count}");

                        await DisplayAlert("Sukces", "Czynno�� zosta�a usuni�ta.", "OK");
                    }
                    else
                    {
                        Debug.WriteLine("Metoda DeletePatientActionAsync zwr�ci�a false. Czynno�� nie zosta�a usuni�ta.");
                        await DisplayAlert("B��d", "Nie uda�o si� usun�� czynno�ci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B��d podczas usuwania czynno�ci: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await DisplayAlert("B��d", "Wyst�pi� problem podczas usuwania czynno�ci.", "OK");
                }
            }
            else
            {
                Debug.WriteLine("Nie uda�o si� pobra� danych czynno�ci z CommandParameter.");
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


        private void UpdateCountdown(object sender, System.Timers.ElapsedEventArgs e)
        {
            _remainingTimeInSeconds--;
            Dispatcher.Dispatch(() =>
            {
                int minutes = _remainingTimeInSeconds / 60;
                int seconds = _remainingTimeInSeconds % 60;
            //    LogoutTimer.Text = $"{minutes:D2}:{seconds:D2}";

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

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
