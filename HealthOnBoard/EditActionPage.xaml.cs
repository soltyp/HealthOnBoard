using HospitalManagementData;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;

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

        public EditActionPage(PatientActivity activity, DatabaseService databaseService)
        {
            InitializeComponent();

            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            if (_activity.LogID == 0)
            {
                DisplayAlert("B³¹d", "Nieprawid³owy LogID. Nie mo¿na edytowaæ tej akcji.", "OK");
                Navigation.PopAsync();
                return;
            }

            // Inicjalizacja danych
            ActionType = _activity.ActionType;
            ActionDetails = _activity.ActionDetails;
            CurrentTemperature = _activity.CurrentTemperature?.ToString("F1") ?? string.Empty;

            // Ustawienie widocznoœci pól
            IsTemperatureInputVisible = _activity.ActionType == "Pomiar temperatury";
            IsDetailsInputVisible = !IsTemperatureInputVisible;

            BindingContext = this;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (_activity.LogID <= 0)
                {
                    await DisplayAlert("B³¹d", "LogID jest nieprawid³owy. Nie mo¿na zapisaæ zmian.", "OK");
                    return;
                }

                // Przygotowanie danych do aktualizacji
                string updatedActionDetails = DetailsEntry.Text; // Szczegó³y wprowadzone przez u¿ytkownika
                decimal? updatedTemperature = null;

                // Jeœli to jest akcja "Pomiar temperatury", pobierz now¹ temperaturê
                if (IsTemperatureInputVisible && decimal.TryParse(TemperatureEntry.Text, out var newTemperature))
                {
                    updatedTemperature = newTemperature;
                }

                // Wywo³anie funkcji aktualizuj¹cej
                bool success = await _databaseService.UpdateActivityLogAsync(
                    _activity.LogID,
                    _activity.ActionType,
                    updatedActionDetails,
                    updatedTemperature
                );

                if (success)
                {
                    await DisplayAlert("Sukces", "Dane zosta³y zapisane.", "OK");
                    await Navigation.PopAsync(); // Powrót do poprzedniej strony
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zaktualizowaæ danych.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas zapisu: {ex.Message}");
                await DisplayAlert("B³¹d", $"Wyst¹pi³ problem: {ex.Message}", "OK");
            }
        }


        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
