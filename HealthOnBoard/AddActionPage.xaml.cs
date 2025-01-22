using HospitalManagementData;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;

namespace HealthOnBoard
{
    public partial class AddActionPage : ContentPage
    {
        private readonly int _patientId;
        private readonly int _userId;
        private readonly DatabaseService _databaseService;
        private readonly Action _onActionAdded;
        private int QuantityValue { get; set; } = 1;
        private System.Timers.Timer _logoutTimer;

        private int _remainingTimeInSeconds = 180; // 3 minuty (180 sekund)

        private void InitializeLogoutTimer()
        {
            _logoutTimer = new System.Timers.Timer(1000); // 1 sekunda
            _logoutTimer.Elapsed += (sender, e) => UpdateCountdown();
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }

        private void UpdateCountdown()
        {
            // Aktualizuj licznik
            _remainingTimeInSeconds--;

            // Wykonaj aktualizacjê na w¹tku UI
            Dispatcher.Dispatch(() =>
            {
                int minutes = _remainingTimeInSeconds / 60;
                int seconds = _remainingTimeInSeconds % 60;

                LogoutTimer.Text = $"{minutes:D2}:{seconds:D2}";

                // Jeœli licznik siê skoñczy, zatrzymaj timer i wyloguj
                if (_remainingTimeInSeconds <= 0)
                {
                    _logoutTimer?.Stop();
                    LogoutUser();
                }
            });
        }

        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> Units { get; set; } = new ObservableCollection<string> { "sztuka", "ml", "mg" };
        public ObservableCollection<Medication> FilteredMedications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> ActionTypes { get; set; } = new ObservableCollection<string>
        {
            "Dodanie wyników badañ",
            "Aktualizacja leczenia",
            "Podanie leków",
            "Dodanie komentarza",
            "Pomiar temperatury"
        };

        public AddActionPage(int patientId, int userId, DatabaseService databaseService, Action onActionAdded)
        {
            InitializeComponent();
            _patientId = patientId;
            _userId = userId;
            _databaseService = databaseService;
            _onActionAdded = onActionAdded;
            BindingContext = this;

            _logoutTimer = new System.Timers.Timer(1800); // Initialize timer
            _logoutTimer.Elapsed += (sender, e) => UpdateCountdown();
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();

            // LoadAlphabetButtons(); // Generowanie przycisków alfabetu
            LoadMedications();     // £adowanie leków
            InitializeLogoutTimer(); // Dodaj to wywo³anie
        }
        private void OnPageTapped(object sender, EventArgs e)
        {
            ResetLogoutTimer();
        }

        private async void LoadMedications()
        {
            var medications = await _databaseService.GetMedicationsAsync();
            foreach (var medication in medications)
            {
                Medications.Add(medication);
                FilteredMedications.Add(medication); // Kopia dla filtrowania
            }
        }
        private void OnAlphabetFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string letter = button.Text.ToUpper();

                // Filtruj leki zaczynaj¹ce siê na klikniêt¹ literê
                var filtered = Medications.Where(m => m.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase)).ToList();

                FilteredMedications.Clear();
                foreach (var med in filtered)
                {
                    FilteredMedications.Add(med);
                }

                Debug.WriteLine($"Wyfiltrowano leki dla litery: {letter}. Liczba wyników: {FilteredMedications.Count}");
            }
        }

        private void LoadAlphabetButtons()
        {
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int maxPerRow = 5; // Maksymalna liczba przycisków w jednym wierszu

            // Tymczasowy stos do przechowywania liter w rzêdzie
            StackLayout currentRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center
            };

            for (int i = 0; i < alphabet.Length; i++)
            {
                char letter = alphabet[i];

                // Tworzenie przycisku dla litery
                Button button = new Button
                {
                    Text = letter.ToString(),
                    FontSize = 20,
                    BackgroundColor = Color.FromArgb("#8854D0"),
                    TextColor = Colors.White,
                    WidthRequest = 50,
                    HeightRequest = 50,
                    Margin = new Thickness(5, 0)
                };

                // Obs³uga klikniêcia przycisku
                button.Clicked += OnAlphabetFilterClicked;

                // Dodawanie przycisku do bie¿¹cego wiersza
                currentRow.Children.Add(button);

                // Jeœli osi¹gnêliœmy maksymaln¹ liczbê przycisków w rzêdzie lub koniec alfabetu
                if (currentRow.Children.Count == maxPerRow || i == alphabet.Length - 1)
                {
                    // Dodajemy wiersz do kontenera i tworzymy nowy wiersz
                    AlphabetButtonsContainer.Children.Add(currentRow);

                    // Nowy wiersz na kolejne litery
                    currentRow = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Spacing = 10,
                        HorizontalOptions = LayoutOptions.Center
                    };
                }
            }
        }


        private void OnIncreaseQuantityClicked(object sender, EventArgs e)
        {
            QuantityValue++;
            UpdateQuantityLabel();
        }

        private void OnDecreaseQuantityClicked(object sender, EventArgs e)
        {
            if (QuantityValue > 1)
            {
                QuantityValue--;
                UpdateQuantityLabel();
            }
        }

        private void UpdateQuantityLabel()
        {
            QuantityLabel.Text = $"Iloœæ: {QuantityValue}";
        }

        private void OnPresetQuantityClicked(object sender, EventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Text, out int quantity))
            {
                QuantityValue = quantity;
                UpdateQuantityLabel();
            }
        }

        private async void OnSaveActionClicked(object sender, EventArgs e)
        {
            string selectedActionType = ActionTypePicker.SelectedItem as string;
            string actionDetails = ActionDetailsEditor.Text;

            if (string.IsNullOrWhiteSpace(selectedActionType))
            {
                await DisplayAlert("B³¹d", "Proszê wybraæ typ akcji.", "OK");
                return;
            }

            if (selectedActionType == "Pomiar temperatury")
            {
                if (string.IsNullOrWhiteSpace(TemperatureEntry.Text))
                {
                    await DisplayAlert("B³¹d", "Proszê wprowadziæ temperaturê.", "OK");
                    return;
                }

                if (!decimal.TryParse(TemperatureEntry.Text, out decimal temperature))
                {
                    await DisplayAlert("B³¹d", "Wprowadzona temperatura musi byæ liczb¹.", "OK");
                    return;
                }

                bool patientUpdateSuccess = await _databaseService.UpdatePatientTemperatureAsync(_patientId, temperature);

                if (patientUpdateSuccess)
                {
                    string activityDetails = $"Zmierzono temperaturê: {temperature}°C";
                    bool logSuccess = await _databaseService.AddPatientActivityLogAsync(
                        userId: _userId,
                        patientId: _patientId,
                        actionType: selectedActionType,
                        actionDetails: activityDetails,
                        actionDate: DateTime.Now,
                        currentTemperature: temperature
                    );

                    if (logSuccess)
                    {
                        await DisplayAlert("Sukces", "Temperatura zosta³a zaktualizowana i zapisano akcjê.", "OK");
                        _onActionAdded?.Invoke();
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ akcji w logu.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ temperatury pacjenta.", "OK");
                }

                return;
            }

            if (selectedActionType == "Podanie leków")
            {
                var selectedMedication = MedicationPicker.SelectedItem as Medication;
                var selectedUnit = UnitPicker.SelectedItem as string;

                if (selectedMedication == null || selectedUnit == null)
                {
                    await DisplayAlert("B³¹d", "Proszê wybraæ lek i jednostkê.", "OK");
                    return;
                }

                string medicationDetails = $"{selectedMedication.Name}, Iloœæ: {QuantityValue} {selectedUnit}";

                bool medicationSuccess = await _databaseService.AddPatientActionAsync(
                    userID: _userId,
                    patientID: _patientId,
                    actionType: selectedActionType,
                    actionDetails: medicationDetails,
                    actionDate: DateTime.Now
                );

                if (medicationSuccess)
                {
                    await DisplayAlert("Sukces", "Podanie leków zosta³o zapisane pomyœlnie.", "OK");
                    _onActionAdded?.Invoke();
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ podania leków.", "OK");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(actionDetails))
            {
                await DisplayAlert("B³¹d", "Proszê wprowadziæ szczegó³y akcji.", "OK");
                return;
            }

            bool success = await _databaseService.AddPatientActionAsync(
                userID: _userId,
                patientID: _patientId,
                actionType: selectedActionType,
                actionDetails: actionDetails,
                actionDate: DateTime.Now
            );

            if (success)
            {
                await DisplayAlert("Sukces", "Akcja zosta³a dodana pomyœlnie.", "OK");
                _onActionAdded?.Invoke();
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê dodaæ akcji.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz siê wylogowaæ?", "Tak", "Nie");
            if (confirmLogout)
            {
                await Navigation.PopToRootAsync();

                // ZnajdŸ LoginPage w stosie nawigacyjnym i zresetuj pole PIN
                if (Application.Current?.MainPage is NavigationPage navigationPage &&
                    navigationPage.RootPage is LoginPage loginPage)
                {
                    loginPage.ClearPin();
                }
            }
        }
        private void OnActionTypeChanged(object sender, EventArgs e)
        {
            var selectedActionType = ActionTypePicker.SelectedItem as string;
            TemperatureEntry.IsVisible = selectedActionType == "Pomiar temperatury";
            MedicationControls.IsVisible = selectedActionType == "Podanie leków";
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logoutTimer?.Stop();
        }


        //private void AddTapGestureToMainGrid()
        //{
        //    var tapGesture = new TapGestureRecognizer();
        //    tapGesture.Tapped += OnScreenTapped;
        //    MainGrid.GestureRecognizers.Add(tapGesture);
        //}

        private async void LogoutUser()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
                await Navigation.PopToRootAsync();

                // ZnajdŸ LoginPage w stosie nawigacyjnym
                if (Application.Current?.MainPage is NavigationPage navigationPage &&
                    navigationPage.RootPage is LoginPage loginPage)
                {
                    loginPage.ClearPin();
                }
            });
        }

        private void OnScreenTapped(object sender, EventArgs e)
        {
            ResetLogoutTimer();
        }
        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = 180; // Reset do 3 minut
            if (_logoutTimer == null)
            {
                InitializeLogoutTimer();
            }
            else
            {
                _logoutTimer.Start();
            }
        }


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
    }
}
