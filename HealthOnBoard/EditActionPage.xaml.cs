using HospitalManagementData;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;

namespace HealthOnBoard
{
    public partial class EditActionPage : ContentPage
    {
        private readonly PatientActivity _activity;
        private readonly DatabaseService _databaseService;

        public string ActionDetails { get; set; }
        public string ActionType { get; set; }
        public bool IsTemperatureInputVisible { get; set; }
        public bool IsDetailsInputVisible { get; set; }
        public string CurrentTemperature { get; set; }
        public bool IsDrugAdministrationVisible { get; set; }

        public ObservableCollection<Medication> Medications { get; set; } = new ObservableCollection<Medication>();
        public ObservableCollection<string> MedicationUnits { get; set; } = new ObservableCollection<string> { "sztuka", "ml", "mg" };
        public ObservableCollection<Medication> FilteredMedications { get; set; } = new ObservableCollection<Medication>();

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
        // Add this to your `EditActionPage` class
        private System.Timers.Timer _logoutTimer;

        private const int LogoutTimeInSeconds = 180; // 3 minutes
        private int _remainingTimeInSeconds;

        // Initialize and start the timer
        private void InitializeLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;

            // Explicitly specify System.Timers.Timer
            _logoutTimer = new System.Timers.Timer(1000); // 1-second intervals
            _logoutTimer.Elapsed += UpdateCountdown;
            _logoutTimer.AutoReset = true;
            _logoutTimer.Start();
        }
        // Method to update the countdown timer
        private void UpdateCountdown(object sender, ElapsedEventArgs e)
        {
            // Decrement the remaining time
            _remainingTimeInSeconds--;

            // Update the UI on the main thread
            Dispatcher.Dispatch(() =>
            {
                int minutes = _remainingTimeInSeconds / 60;
                int seconds = _remainingTimeInSeconds % 60;
                LogoutTimerLabel.Text = $"{minutes:D2}:{seconds:D2}";

                // If time runs out, stop the timer and log the user out
                if (_remainingTimeInSeconds <= 0)
                {
                    _logoutTimer.Stop();
                    LogoutUser();
                }
            });
        }

        // Reset the timer when activity occurs
        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
        }

        // Log the user out when the timer expires
        private async void LogoutUser()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("Session Expired", "Your session has expired due to inactivity.", "OK");
                await Navigation.PopToRootAsync();
            });
        }
        public EditActionPage(PatientActivity activity, DatabaseService databaseService)
        {
            InitializeComponent();

            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            InitializeLogoutTimer();
            // Set section visibility
            IsDrugAdministrationVisible = _activity.ActionType == "Podanie leków";
            IsTemperatureInputVisible = _activity.ActionType == "Pomiar temperatury";
            IsDetailsInputVisible = !_activity.ActionType.Contains("Pomiar temperatury") && !_activity.ActionType.Contains("Podanie leków");

            // Set initial values
            ActionType = _activity.ActionType;
            ActionDetails = _activity.ActionDetails;
            CurrentTemperature = _activity.CurrentTemperature?.ToString("F1") ?? string.Empty;

            if (IsDrugAdministrationVisible)
            {
                LoadMedicationsAsync();
                PopulateMedicationFields();
            }

            BindingContext = this;
        }
        private void OnPageTapped(object sender, EventArgs e)
        {
            // Reset the logout timer when the page is tapped
            ResetLogoutTimer();
            Debug.WriteLine("Logout timer reset due to user interaction.");
        }

        public string PreviousMedication { get; set; } = "Brak"; // Default to "Brak"

        private void PopulateMedicationFields()
        {
            if (string.IsNullOrEmpty(_activity.ActionDetails))
            {
                Debug.WriteLine("No details for drug administration.");
                return;
            }

            try
            {
                var actionDetails = _activity.ActionDetails.Split(',');
                if (actionDetails.Length > 1)
                {
                    var medicationName = actionDetails[0].Replace("Podano lek:", "").Trim();
                    var quantityUnit = actionDetails[1].Split(':');

                    // Assign medication name to the property
                    PreviousMedication = medicationName;

                    // Set the default medication in the picker
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name.Equals(medicationName, StringComparison.OrdinalIgnoreCase));

                    // If the medication is not in the filtered list, temporarily add it
                    if (SelectedMedication != null && !FilteredMedications.Contains(SelectedMedication))
                    {
                        FilteredMedications.Add(SelectedMedication);
                    }

                    // Assign quantity and unit
                    SelectedQuantity = int.TryParse(quantityUnit[1].Split(' ')[1], out var quantity) ? quantity : 1;
                    SelectedUnit = quantityUnit[1].Split(' ')[2];

                    Debug.WriteLine($"Medication: {PreviousMedication}, Quantity: {SelectedQuantity}, Unit: {SelectedUnit}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error populating medication fields: {ex.Message}");
            }
        }

        private async void LoadMedicationsAsync()
        {
            try
            {
                var medications = await _databaseService.GetMedicationsAsync();
                Medications.Clear();
                FilteredMedications.Clear();

                foreach (var medication in medications)
                {
                    Medications.Add(medication);
                    FilteredMedications.Add(medication); // Show the full list initially
                }

                // Set the assigned medication as selected
                if (!string.IsNullOrEmpty(PreviousMedication))
                {
                    SelectedMedication = Medications.FirstOrDefault(m => m.Name.Equals(PreviousMedication, StringComparison.OrdinalIgnoreCase));
                }

                PopulateMedicationFields();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading medications: {ex.Message}");
                await DisplayAlert("Error", "Failed to load medication list.", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Assign values based on the action type
                if (_activity.ActionType == "Pomiar temperatury" && decimal.TryParse(TemperatureEntry.Text, out var temperature))
                {
                    _activity.CurrentTemperature = temperature;
                }
                else if (_activity.ActionType == "Podanie leków" && SelectedMedication != null)
                {
                    _activity.ActionDetails = $"Podano lek: {SelectedMedication.Name}, iloœæ: {SelectedQuantity} {SelectedUnit}";
                }
                else
                {
                    _activity.ActionDetails = ActionDetails;
                }

                // Update the database
                var success = await _databaseService.UpdateActivityLogAsync(
                    _activity.LogID,
                    _activity.ActionType,
                    _activity.ActionDetails,
                    _activity.CurrentTemperature
                );

                if (success)
                {
                    Debug.WriteLine("Changes saved, notifying history refresh...");

                    // Send update notification
                    MessagingCenter.Send(this, "RefreshPatientActivityHistory");

                    await DisplayAlert("Success", "Data saved successfully.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to update data.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving data: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void OnAlphabetFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.Text is not null)
            {
                string selectedLetter = button.Text;

                // Filter medications based on the selected letter
                FilteredMedications.Clear();

                foreach (var medication in Medications)
                {
                    if (medication.Name.StartsWith(selectedLetter, StringComparison.OrdinalIgnoreCase))
                    {
                        FilteredMedications.Add(medication);
                    }
                }

                Debug.WriteLine($"Filtered medications by letter: {selectedLetter}. Found {FilteredMedications.Count} medications.");

                // Automatically select the first medication in the list, if any
                SelectedMedication = FilteredMedications.FirstOrDefault();
            }
        }

        private void DecreaseQuantity_Clicked(object sender, EventArgs e)
        {
            if (SelectedQuantity > 1)
            {
                SelectedQuantity--;
            }
        }

        private void IncreaseQuantity_Clicked(object sender, EventArgs e)
        {
            SelectedQuantity++;
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Confirmation", "Are you sure you want to log out?", "Yes", "No");
            if (confirmLogout)
            {
                await Navigation.PopToRootAsync();
            }
        }
    }
}
