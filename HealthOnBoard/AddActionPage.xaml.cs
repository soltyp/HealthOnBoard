using HospitalManagementData;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class AddActionPage : ContentPage
    {
        private readonly int _patientId;
        private readonly int _userId;
        private readonly DatabaseService _databaseService;
        private readonly Action _onActionAdded;
        private int QuantityValue { get; set; } = 1;

        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> Units { get; set; } = new ObservableCollection<string> { "mg", "sztuka", "tabletka" };
        public ObservableCollection<Medication> FilteredMedications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> ActionTypes { get; set; } = new ObservableCollection<string>
        {
            "Dodanie wynik�w bada�",
            "Aktualizacja leczenia",
            "Podanie lek�w",
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

           // LoadAlphabetButtons(); // Generowanie przycisk�w alfabetu
            LoadMedications();     // �adowanie lek�w
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

                // Filtruj leki zaczynaj�ce si� na klikni�t� liter�
                var filtered = Medications.Where(m => m.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase)).ToList();

                FilteredMedications.Clear();
                foreach (var med in filtered)
                {
                    FilteredMedications.Add(med);
                }

                Debug.WriteLine($"Wyfiltrowano leki dla litery: {letter}. Liczba wynik�w: {FilteredMedications.Count}");
            }
        }

        private void LoadAlphabetButtons()
        {
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int maxPerRow = 5; // Maksymalna liczba przycisk�w w jednym wierszu

            // Tymczasowy stos do przechowywania liter w rz�dzie
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

                // Obs�uga klikni�cia przycisku
                button.Clicked += OnAlphabetFilterClicked;

                // Dodawanie przycisku do bie��cego wiersza
                currentRow.Children.Add(button);

                // Je�li osi�gn�li�my maksymaln� liczb� przycisk�w w rz�dzie lub koniec alfabetu
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
            QuantityLabel.Text = $"Ilo��: {QuantityValue}";
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
                await DisplayAlert("B��d", "Prosz� wybra� typ akcji.", "OK");
                return;
            }

            if (selectedActionType == "Pomiar temperatury")
            {
                if (string.IsNullOrWhiteSpace(TemperatureEntry.Text))
                {
                    await DisplayAlert("B��d", "Prosz� wprowadzi� temperatur�.", "OK");
                    return;
                }

                if (!decimal.TryParse(TemperatureEntry.Text, out decimal temperature))
                {
                    await DisplayAlert("B��d", "Wprowadzona temperatura musi by� liczb�.", "OK");
                    return;
                }

                bool patientUpdateSuccess = await _databaseService.UpdatePatientTemperatureAsync(_patientId, temperature);

                if (patientUpdateSuccess)
                {
                    string activityDetails = $"Zmierzono temperatur�: {temperature}�C";
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
                        await DisplayAlert("Sukces", "Temperatura zosta�a zaktualizowana i zapisano akcj�.", "OK");
                        _onActionAdded?.Invoke();
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("B��d", "Nie uda�o si� zapisa� akcji w logu.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("B��d", "Nie uda�o si� zaktualizowa� temperatury pacjenta.", "OK");
                }

                return;
            }

            if (selectedActionType == "Podanie lek�w")
            {
                var selectedMedication = MedicationPicker.SelectedItem as Medication;
                var selectedUnit = UnitPicker.SelectedItem as string;

                if (selectedMedication == null || selectedUnit == null)
                {
                    await DisplayAlert("B��d", "Prosz� wybra� lek i jednostk�.", "OK");
                    return;
                }

                string medicationDetails = $"{selectedMedication.Name}, Ilo��: {QuantityValue} {selectedUnit}";

                bool medicationSuccess = await _databaseService.AddPatientActionAsync(
                    userID: _userId,
                    patientID: _patientId,
                    actionType: selectedActionType,
                    actionDetails: medicationDetails,
                    actionDate: DateTime.Now
                );

                if (medicationSuccess)
                {
                    await DisplayAlert("Sukces", "Podanie lek�w zosta�o zapisane pomy�lnie.", "OK");
                    _onActionAdded?.Invoke();
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("B��d", "Nie uda�o si� zapisa� podania lek�w.", "OK");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(actionDetails))
            {
                await DisplayAlert("B��d", "Prosz� wprowadzi� szczeg�y akcji.", "OK");
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
                await DisplayAlert("Sukces", "Akcja zosta�a dodana pomy�lnie.", "OK");
                _onActionAdded?.Invoke();
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("B��d", "Nie uda�o si� doda� akcji.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnActionTypeChanged(object sender, EventArgs e)
        {
            var selectedActionType = ActionTypePicker.SelectedItem as string;
            TemperatureEntry.IsVisible = selectedActionType == "Pomiar temperatury";
            MedicationControls.IsVisible = selectedActionType == "Podanie lek�w";
        }
    }
}
