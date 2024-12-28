using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using HospitalManagementData;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;


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
                    Debug.WriteLine($"Wczytano aktywno��: LogID={activity.LogID}, ActionType={activity.ActionType}");
                    activity.PatientID = _patient.PatientID; // Upewnij si�, �e PatientID jest poprawnie przypisane
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
            // Pobranie temperatury pacjenta
            await LoadPatientTemperatureAsync();

            await LoadAssignedDrugsAsync(); // Load assigned drugs
            await LoadPatientNotesAsync(); // Load patient notes
            await LoadTemperatureChartDataAsync(); // Nowa metoda
        }

        private async Task LoadTemperatureChartDataAsync()
        {
            try
            {
                // Pobierz dane o temperaturze z bazy danych
                var temperatureLogs = await _databaseService.GetTemperatureLogsAsync(_patient.PatientID);

                // Wybierz ostatnie 5 warto�ci temperatury, posortowane rosn�co po dacie
                var lastFiveTemperatures = temperatureLogs
                                           .OrderByDescending(t => t.ActionDate)
                                           .Take(5) // Pobierz 5 najnowszych
                                           .OrderBy(t => t.ActionDate) // Posortuj od najstarszej do najnowszej
                                           .ToList();

                // Przeka� te dane do metody tworz�cej wykres
                BuildTemperatureChart(lastFiveTemperatures);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania danych do wykresu: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� za�adowa� wykresu temperatury.", "OK");
            }
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

                        // Od�wie� wykres temperatury, je�li usuni�to wiersz zwi�zany z temperatur�
                        if (activity.ActionType == "Pomiar temperatury")
                        {
                            await LoadTemperatureChartDataAsync();
                        }

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
                // Rozpoznaj, czy akcja dotyczy pomiaru temperatury
                if (activity.ActionType == "Pomiar temperatury")
                {
                    try
                    {
                        // Pobierz warto�� temperatury z kolumny CurrentTemperature
                        decimal? currentTemperature = activity.CurrentTemperature;

                        if (currentTemperature == null)
                        {
                            await DisplayAlert("B��d", "Nie znaleziono warto�ci temperatury w bazie danych.", "OK");
                            return;
                        }

                        // Wy�wietl dialog do edycji temperatury
                        string newTemperature = await DisplayPromptAsync(
                            "Edytuj pomiar temperatury",
                            "Wprowad� now� warto�� temperatury (w �C):",
                            keyboard: Keyboard.Numeric,
                            initialValue: currentTemperature.Value.ToString("F1") // Formatowanie do 1 miejsca po przecinku
                        );

                        // Walidacja nowej temperatury
                        if (decimal.TryParse(newTemperature, out decimal temperatureValue) && temperatureValue >= 35 && temperatureValue <= 42)
                        {
                            activity.ActionDetails = $"Zmieniono temperatur� na: {temperatureValue:F1}�C";

                            // Zaktualizuj temperatur� w bazie danych
                            bool tempUpdateSuccess = await _databaseService.UpdatePatientTemperatureAsync(activity.PatientID, temperatureValue);
                            bool activityUpdateSuccess = await _databaseService.UpdatePatientActionAsync(activity);
                            bool logUpdateSuccess = await _databaseService.UpdateActivityLogTemperatureAsync(activity.LogID, temperatureValue);

                            if (tempUpdateSuccess && activityUpdateSuccess && logUpdateSuccess)
                            {
                                await DisplayAlert("Sukces", "Pomiar temperatury zosta� zaktualizowany.", "OK");

                                // Od�wie� list� aktywno�ci
                                await LoadRecentActivitiesAsync();

                                // Od�wie� wykres
                                await LoadTemperatureChartDataAsync();
                            }
                            else
                            {
                                string errorDetails = $"tempUpdate: {tempUpdateSuccess}, activityUpdate: {activityUpdateSuccess}, logUpdate: {logUpdateSuccess}";
                                Debug.WriteLine($"Nie uda�o si� w pe�ni zaktualizowa� danych. Szczeg�y: {errorDetails}");
                                await DisplayAlert("B��d", $"Nie uda�o si� w pe�ni zaktualizowa� danych. Szczeg�y: {errorDetails}", "OK");
                            }
                        }
                        else
                        {
                            await DisplayAlert("B��d", "Wprowad� poprawn� warto�� temperatury (35-42�C).", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B��d podczas edycji temperatury: {ex.Message}");
                        await DisplayAlert("B��d", "Wyst�pi� problem podczas edycji pomiaru temperatury.", "OK");
                    }
                }
                else
                {
                    // Dla innych typ�w akcji
                    string newActionDetails = await DisplayPromptAsync(
                        "Edytuj szczeg�y czynno�ci",
                        "Zmie� szczeg�y czynno�ci:",
                        initialValue: activity.ActionDetails
                    );

                    if (!string.IsNullOrWhiteSpace(newActionDetails))
                    {
                        activity.ActionDetails = newActionDetails;

                        try
                        {
                            // Aktualizuj w bazie danych
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



        private async void OnMorePatientDataClicked(object sender, EventArgs e)
        {
            // Pass the required parameters to the PatientDetailsPage
            await Navigation.PushAsync(new PatientDetailsPage(_user, _patient.PatientID, _databaseService));
        }



        private bool _isListVisible = true;

        private async void ToggleListVisibility(object sender, EventArgs e)
        {
            _isListVisible = !_isListVisible;
            RecentActivitiesList.IsVisible = _isListVisible;

            // Animacja klikni�cia
            var frame = sender as Frame;
            await frame.ScaleTo(0.95, 50);
            await frame.ScaleTo(1, 50);

            // Zmie� tekst
            ToggleButtonLabel.Text = _isListVisible ? "Ukryj list�" : "Poka� list�";
        }




        private async void OnAddActionPageClicked(object sender, EventArgs e)
        {
            // Otw�rz now� stron� AddActionPage
            await Navigation.PushAsync(new AddActionPage(_patient.PatientID, _user.UserID, _databaseService, async () =>
            {
                // Od�wie� list� aktywno�ci po dodaniu akcji
                await LoadRecentActivitiesAsync();
            }));
        }


        private async Task LoadPatientTemperatureAsync()
        {
            try
            {
                // Pobierz temperatur� pacjenta z tabeli Patients
                var temperature = await _databaseService.GetCurrentTemperatureAsync(_patient.PatientID);

                if (temperature.HasValue)
                {
                    PatientTemperatureLabel.Text = $"{temperature.Value:F1}�C"; // Formatowanie do 1 miejsca po przecinku
                }
                else
                {
                    PatientTemperatureLabel.Text = "Brak danych"; // Gdy brak temperatury w bazie
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas pobierania temperatury pacjenta: {ex.Message}");
                PatientTemperatureLabel.Text = "B��d";
            }
        }


        private async Task LoadAssignedDrugsAsync()
        {
            try
            {
                // Fetch assigned drugs from the database
                var assignedDrugs = await _databaseService.GetAssignedDrugsAsync(_patient.PatientID);

                if (!string.IsNullOrEmpty(assignedDrugs))
                {
                    AssignedDrugsLabel.Text = assignedDrugs;
                }
                else
                {
                    AssignedDrugsLabel.Text = "Brak danych"; // Default if no drugs are assigned
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas pobierania przypisanych lek�w: {ex.Message}");
                AssignedDrugsLabel.Text = "B��d";
            }
        }

        private async Task LoadPatientNotesAsync()
        {
            try
            {
                // Fetch notes from the database
                var notes = await _databaseService.GetPatientNotesAsync(_patient.PatientID);

                if (!string.IsNullOrEmpty(notes))
                {
                    PatientNotesLabel.Text = notes;
                }
                else
                {
                    PatientNotesLabel.Text = "Brak danych"; // Default if no notes are present
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas pobierania uwag pacjenta: {ex.Message}");
                PatientNotesLabel.Text = "B��d";
            }
        }


        private void BuildTemperatureChart(List<(DateTime ActionDate, decimal Temperature)> temperatureLogs)
        {
            // Wyczy�� istniej�c� siatk�
            TemperatureChartGrid.Children.Clear();
            TemperatureChartGrid.ColumnDefinitions.Clear();

            if (temperatureLogs == null || !temperatureLogs.Any())
            {
                Debug.WriteLine("Brak danych do wy�wietlenia na wykresie.");
                return;
            }

            // Oblicz minimaln� i maksymaln� temperatur� do proporcjonalnego skalowania
            decimal minTemperature = temperatureLogs.Min(t => t.Temperature);
            decimal maxTemperature = temperatureLogs.Max(t => t.Temperature);

            // Zabezpieczenie przed dzieleniem przez zero
            if (maxTemperature == minTemperature)
                maxTemperature = minTemperature + 1;

            // Dodanie kolumn do siatki dla ka�dego punktu (i odst�p�w)
            foreach (var _ in temperatureLogs)
            {
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Odst�p mi�dzy punktami
            }

            int columnIndex = 0;
            double maxChartHeight = 250; // Zwi�kszona wysoko�� obszaru dla punkt�w

            foreach (var log in temperatureLogs)
            {
                // Obliczenie proporcjonalnej wysoko�ci punktu
                double verticalPosition = (double)((log.Temperature - minTemperature) / (maxTemperature - minTemperature)) * maxChartHeight;

                // Punkt na wykresie
                var point = new Ellipse
                {
                    Fill = new SolidColorBrush(Color.FromHex("#FF5733")),
                    HeightRequest = 12,
                    WidthRequest = 12,
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, maxChartHeight - verticalPosition + 20, 0, 0) // Przesuni�cie o dodatkowe 20 jednostek w g�r�
                };

                // Etykieta temperatury nad punktem
                var tempLabel = new Label
                {
                    Text = $"{log.Temperature:F1}�C",
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Start,
                    TextColor = Colors.White,
                    FontSize = 12,
                    Margin = new Thickness(0, maxChartHeight - verticalPosition - 10, 0, 0) // Przesuni�cie nad punkt
                };

                // Etykieta daty i godziny pod punktem
                var dateLabel = new Label
                {
                    Text = log.ActionDate.ToString("dd-MM HH:mm"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.End,
                    TextColor = Colors.Gray,
                    FontSize = 10,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                // Dodanie element�w do siatki
                TemperatureChartGrid.Children.Add(tempLabel);
                Grid.SetColumn(tempLabel, columnIndex);
                Grid.SetRow(tempLabel, 0);

                TemperatureChartGrid.Children.Add(point);
                Grid.SetColumn(point, columnIndex);
                Grid.SetRow(point, 0);

                TemperatureChartGrid.Children.Add(dateLabel);
                Grid.SetColumn(dateLabel, columnIndex);
                Grid.SetRow(dateLabel, 1);

                columnIndex += 2; // Przeskok o dwie kolumny
            }
        }






        private void BuildTemperatureBarChart(Dictionary<string, int> temperatureData)
        {
            TemperatureChartGrid.Children.Clear();
            TemperatureChartGrid.ColumnDefinitions.Clear();

            foreach (var _ in temperatureData)
            {
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            int maxCount = temperatureData.Values.Max();
            int columnIndex = 0;

            foreach (var item in temperatureData)
            {
                var bar = new BoxView
                {
                    Color = GetRandomColor(),
                    HeightRequest = (item.Value / (double)maxCount) * 200, // Skalowanie wysoko�ci
                    VerticalOptions = LayoutOptions.End
                };

                var label = new Label
                {
                    Text = item.Key, // Temperatura
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                var countLabel = new Label
                {
                    Text = item.Value.ToString(), // Ilo��
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.End
                };

                TemperatureChartGrid.Children.Add(bar);
                Grid.SetColumn(bar, columnIndex);
                Grid.SetRow(bar, 0);

                TemperatureChartGrid.Children.Add(label);
                Grid.SetColumn(label, columnIndex);
                Grid.SetRow(label, 1);

                TemperatureChartGrid.Children.Add(countLabel);
                Grid.SetColumn(countLabel, columnIndex);
                Grid.SetRow(countLabel, 0);

                columnIndex++;
            }
        }

        private async void OnShowTemperatureChartClicked(object sender, EventArgs e)
        {
            // Pobierz ID pacjenta i przeka� us�ug� bazy danych
            int patientId = _patient.PatientID;
            await Navigation.PushAsync(new TemperatureChartPage(patientId, _databaseService));
        }

        private async void OnShowPatientHistoryClicked(object sender, EventArgs e)
        {
            // Przyk�ad nawigacji do nowej strony z histori� pacjenta
            await Navigation.PushAsync(new PatientHistoryPage(_user, _patient.PatientID, _databaseService));
        }


        private Color GetRandomColor()
        {
            Random random = new Random();
            return Color.FromRgb(random.Next(50, 200), random.Next(50, 200), random.Next(50, 200));
        }

    }
}