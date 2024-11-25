using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using HospitalManagementData;

namespace HealthOnBoard
{
    public partial class DashboardPage : ContentPage
    {
        private readonly User _user;
        private readonly Patient _patient;
        private readonly DatabaseService _databaseService;
        public ObservableCollection<PatientActivity> RecentActivities { get; set; } = new ObservableCollection<PatientActivity>();


        private System.Timers.Timer _logoutTimer;

        private int _remainingTimeInSeconds = 180; // 3 minuty (180 sekund)

        private void InitializeLogoutTimer()
        {
            _logoutTimer = new System.Timers.Timer(1000); // 1 sekunda
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                var activities = await _databaseService.GetRecentActivitiesAsync(_patient.PatientID);

                RecentActivities.Clear();
                foreach (var activity in activities)
                {
                    // Upewnij si�, �e PatientID jest poprawnie przypisane
                    activity.PatientID = _patient.PatientID;
                    RecentActivities.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas pobierania operacji pacjenta: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� pobra� operacji pacjenta.", "OK");
            }
        }



        public ObservableCollection<string> ActionTypes { get; set; } = new ObservableCollection<string>
        {
            "Dodanie wynik�w bada�",
            "Aktualizacja leczenia",
            "Zmiana danych pacjenta",
            "Podanie lek�w",
            "Dodanie komentarza"
        };

        private void UpdateCountdown(object sender, ElapsedEventArgs e)
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
                await DisplayAlert("Sesja wygas�a", "Twoja sesja wygas�a z powodu braku aktywno�ci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = 180; // Reset czasu do 3 minut
            _logoutTimer?.Start();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRecentActivitiesAsync();
            ResetLogoutTimer();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logoutTimer?.Stop();
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

        public DashboardPage(User user, Patient patient, DatabaseService databaseService)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");
            _patient = patient ?? throw new ArgumentNullException(nameof(patient), "Patient cannot be null");
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "DatabaseService cannot be null");

            if (_patient.PatientID <= 0)
            {
                Debug.WriteLine("B��d: Nieprawid�owy PatientID w obiekcie Patient.");
            }

            // Ustaw dane w interfejsie u�ytkownika
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = _user.Role ?? "Brak roli";

            PatientNameLabel.Text = _patient.Name;
            PatientAgeLabel.Text = _patient.Age.ToString();
            BedNumberLabel.Text = _patient.BedNumber.ToString();


            // Inicjalizacja timera
            InitializeLogoutTimer();

            // Obs�uga dotkni�cia pustych miejsc na ekranie
            AddTapGestureToMainGrid();

            BindingContext = this;
            _databaseService = databaseService;
        }

        private async void OnAddActionClicked(object sender, EventArgs e)
        {
            string selectedActionType = ActionTypePicker.SelectedItem as string;
            string actionDetails = ActionDetailsEditor.Text;

            if (string.IsNullOrWhiteSpace(selectedActionType))
            {
                await DisplayAlert("B��d", "Prosz� wybra� typ akcji.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(actionDetails))
            {
                await DisplayAlert("B��d", "Prosz� wprowadzi� szczeg�y akcji.", "OK");
                return;
            }

            // Tworzenie nowej akcji
            var newAction = new
            {
                UserID = _user.UserID,
                PatientID = _patient.PatientID,
                ActionType = selectedActionType,
                ActionDetails = actionDetails,
                ActionDate = DateTime.Now
            };

            // Wywo�anie zapisania akcji
            bool success = await SaveActionToDatabase(newAction);
            await LoadRecentActivitiesAsync();

            if (success)
            {
                await DisplayAlert("Sukces", "Akcja zosta�a dodana pomy�lnie.", "OK");
                ActionDetailsEditor.Text = string.Empty;
                ActionTypePicker.SelectedIndex = -1;
            }
            else
            {
                await DisplayAlert("B��d", "Nie uda�o si� zapisa� akcji.", "OK");
            }
        }

        private async Task<bool> SaveActionToDatabase(object newAction)
        {
            ResetLogoutTimer();
            try
            {
                var action = (dynamic)newAction;

                bool success = await _databaseService.AddPatientActionAsync(
                    userID: action.UserID,
                    patientID: action.PatientID,
                    actionType: action.ActionType,
                    actionDetails: action.ActionDetails,
                    actionDate: action.ActionDate
                );

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas zapisywania akcji do bazy danych: {ex.Message}");
                return false;
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz si� wylogowa�?", "Tak", "Nie");
            if (confirmLogout)
            {
                await Navigation.PopToRootAsync();
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
                        RecentActivities.Remove(activity);
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
                    Debug.WriteLine($"Wyj�tek podczas usuwania czynno�ci: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await DisplayAlert("B��d", "Wyst�pi� problem podczas usuwania czynno�ci.", "OK");
                }
            }
            else
            {
                Debug.WriteLine("Nie uda�o si� pobra� danych czynno�ci z CommandParameter.");
            }
        }

        private async void OnEditActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                // Wy�wietl Picker do wyboru nowego typu akcji
                string newActionType = await DisplayActionSheet(
                    "Zmie� typ akcji",
                    "Anuluj",
                    null,
                    ActionTypes.ToArray() // Zamieniamy ObservableCollection na tablic� string�w
                );

                // Sprawd�, czy u�ytkownik wybra� nowy typ akcji (anulowanie zwraca null)
                if (!string.IsNullOrWhiteSpace(newActionType) && newActionType != "Anuluj")
                {
                    activity.ActionType = newActionType;
                }

                // Wy�wietl dialog do edycji szczeg��w akcji
                string newActionDetails = await DisplayPromptAsync(
                    "Edytuj szczeg�y czynno�ci",
                    "Zmie� szczeg�y czynno�ci:",
                    initialValue: activity.ActionDetails
                );

                if (!string.IsNullOrWhiteSpace(newActionDetails))
                {
                    try
                    {
                        // Aktualizuj szczeg�y i typ akcji
                        activity.ActionDetails = newActionDetails;

                        // Wywo�aj metod� aktualizacji w bazie
                        bool success = await _databaseService.UpdatePatientActionAsync(activity);

                        if (success)
                        {
                            await DisplayAlert("Sukces", "Czynno�� zosta�a zaktualizowana.", "OK");
                            await LoadRecentActivitiesAsync(); // Od�wie� list� czynno�ci
                        }
                        else
                        {
                            await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� czynno�ci.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B��d podczas edytowania czynno�ci: {ex.Message}");
                        await DisplayAlert("B��d", "Wyst�pi� problem podczas edycji czynno�ci.", "OK");
                    }
                }
            }
        }



    }
}
