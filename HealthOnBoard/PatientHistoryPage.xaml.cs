using HospitalManagementData;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientHistoryPage : ContentPage
    {
        // Lista dostêpnych leków
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

        // Dodaj pola dla userId i patientId, jeœli ich brakuje
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
            _dashboardPage = dashboardPage; // Przechowujemy instancjê DashboardPage
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
                Debug.WriteLine("Rozpoczynam ³adowanie historii pacjenta...");

                // Pobierz dane z bazy danych
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);

                // Posortuj dane wed³ug ActionDate malej¹co
                var sortedActivities = activities.OrderByDescending(activity => activity.ActionDate);

                // Wyczyœæ istniej¹c¹ listê i za³aduj nowe dane
                PatientHistory.Clear();
                foreach (var activity in sortedActivities)
                {
                    if (activity.LogID == 0)
                    {
                        Debug.WriteLine($"B³¹d: LogID jest równy 0 dla czynnoœci: {activity.ActionType}");
                        continue;
                    }
                    PatientHistory.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê pobraæ historii pacjenta.", "OK");
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
                case "Podanie leków":
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
                case "Podanie leków":
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name == activity.ActionDetails);
                    break;
                    // Dodaj inne przypadki w zale¿noœci od potrzeb
            }
        }

        private async Task SaveActivityAsync()
        {
            var newActivity = new PatientActivity
            {
                ActionType = SelectedActionType,
                ActionDetails = SelectedActionType == "Podanie leków" ? SelectedMedication.Name : null,
                CurrentTemperature = SelectedActionType == "Pomiar temperatury" ? decimal.Parse(CurrentTemperature) : null,
                ActionDate = DateTime.Now
            };

            // Zapisz do bazy danych
            var success = await _databaseService.AddPatientActionAsync(userId, patientId, newActivity.ActionType, newActivity.ActionDetails, newActivity.ActionDate);
            if (success)
            {
                await DisplayAlert("Sukces", "Akcja zosta³a zapisana.", "OK");
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ akcji.", "OK");
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
                await DisplayAlert("B³¹d", "Nie uda³o siê pobraæ danych do edycji.", "OK");
            }
        }





        //private async void OnEditActionClicked(object sender, EventArgs e)
        //{
        //    if (sender is Button button && button.CommandParameter is PatientActivity activity)
        //    {
        //        // Obs³uga edycji na podstawie typu akcji
        //        string newValue = await DisplayPromptAsync(
        //            $"Edytuj {activity.ActionType}",
        //            $"WprowadŸ now¹ wartoœæ dla: {activity.ActionDetails}",
        //            initialValue: activity.ActionDetails
        //        );

        //        if (!string.IsNullOrEmpty(newValue))
        //        {
        //            try
        //            {
        //                activity.ActionDetails = newValue; // Zaktualizuj lokaln¹ wartoœæ
        //                bool success = await _databaseService.UpdateActivityDetailsAsync(activity);

        //                if (success)
        //                {
        //                    await DisplayAlert("Sukces", "Dane zosta³y zaktualizowane.", "OK");
        //                    await RefreshPatientHistoryAsync();
        //                }
        //                else
        //                {
        //                    await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ danych w bazie.", "OK");
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"B³¹d podczas aktualizacji danych: {ex.Message}");
        //                await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas aktualizacji danych.", "OK");
        //            }
        //        }
        //        else
        //        {
        //            await DisplayAlert("Anulowano", "Edycja zosta³a anulowana.", "OK");
        //        }
        //    }
        //}







        private async Task RefreshPatientHistoryAsync()
        {
            try
            {
                // Pobierz zaktualizowan¹ historiê pacjenta
                var activities = await _databaseService.GetFullActivitiesAsync(_patientId);

                // Wyczyœæ bie¿¹c¹ listê i za³aduj nowe dane
                PatientHistory.Clear();
                foreach (var activity in activities)
                {
                    PatientHistory.Add(activity);
                }

                Debug.WriteLine("Historia pacjenta zosta³a odœwie¿ona.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas odœwie¿ania historii pacjenta: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê odœwie¿yæ historii pacjenta.", "OK");
            }
        }


       



        private async void OnDeleteActionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PatientActivity activity)
            {
                Debug.WriteLine($"Rozpoczynam usuwanie czynnoœci: {activity.ActionType}, {activity.ActionDetails}, {activity.ActionDate}, PatientID: {activity.PatientID}");

                bool confirmDelete = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz usun¹æ tê czynnoœæ?", "Tak", "Nie");
                if (!confirmDelete)
                {
                    Debug.WriteLine("Usuwanie anulowane przez u¿ytkownika.");
                    return;
                }

                try
                {
                    Debug.WriteLine("Wywo³ywanie metody DeletePatientActionAsync...");
                    bool success = await _databaseService.DeletePatientActionAsync(activity);

                    if (success)
                    {
                        Debug.WriteLine("Czynnoœæ zosta³a usuniêta z bazy danych.");
                        Debug.WriteLine($"Przed usuniêciem: Liczba elementów w PatientHistory: {PatientHistory.Count}");
                        PatientHistory.Remove(activity);
                        Debug.WriteLine($"Po usuniêciu: Liczba elementów w PatientHistory: {PatientHistory.Count}");

                        await DisplayAlert("Sukces", "Czynnoœæ zosta³a usuniêta.", "OK");
                    }
                    else
                    {
                        Debug.WriteLine("Metoda DeletePatientActionAsync zwróci³a false. Czynnoœæ nie zosta³a usuniêta.");
                        await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ czynnoœci.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B³¹d podczas usuwania czynnoœci: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas usuwania czynnoœci.", "OK");
                }
            }
            else
            {
                Debug.WriteLine("Nie uda³o siê pobraæ danych czynnoœci z CommandParameter.");
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
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
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
