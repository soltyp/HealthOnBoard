using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using HospitalManagementData;

namespace HealthOnBoard
{
    public partial class LoginAttemptsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<LoginAttempt> LoginAttempts { get; set; }

        public LoginAttemptsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            LoginAttempts = new ObservableCollection<LoginAttempt>();
            BindingContext = this;

            LoadInitialData(); // Wczytaj dane pocz¹tkowe
        }

        private async void LoadInitialData()
        {
            try
            {
                var attempts = await _databaseService.GetLoginAttemptsAsync();
                foreach (var attempt in attempts)
                {
                    LoginAttempts.Add(attempt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading initial data: {ex.Message}");
            }
        }


        private async void OnFilterClicked(object sender, EventArgs e)
        {
            try
            {
                // Pobierz wartoœci filtrów
                DateTime? selectedDate = DateFilter.Date;
                string selectedUser = string.IsNullOrWhiteSpace(UserFilter.Text) ? null : UserFilter.Text;

                // Wczytaj przefiltrowane dane
                var attempts = await _databaseService.GetLoginAttemptsAsync(selectedDate, selectedUser);

                // Wyczyœæ i dodaj nowe dane do listy
                LoginAttempts.Clear();
                foreach (var attempt in attempts)
                {
                    LoginAttempts.Add(attempt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering login attempts: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê zastosowaæ filtrów.", "OK");
            }


        }

        private async void OnClearFiltersClicked(object sender, EventArgs e)
        {
            try
            {
                // Wyczyœæ filtry w UI
                DateFilter.Date = DateTime.Now;
                UserFilter.Text = string.Empty;

                // Wczytaj wszystkie logi bez filtrowania
                var attempts = await _databaseService.GetLoginAttemptsAsync();

                // Wyczyœæ i dodaj dane do listy
                LoginAttempts.Clear();
                foreach (var attempt in attempts)
                {
                    LoginAttempts.Add(attempt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing filters: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ filtrów.", "OK");
            }
        }

    }



}
