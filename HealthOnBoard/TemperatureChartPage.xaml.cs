using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class TemperatureChartPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly int _patientId;
        private readonly User _user;
        private System.Timers.Timer _logoutTimer;
        private const int LogoutTimeInSeconds = 180;
        private int _remainingTimeInSeconds;

        public TemperatureChartPage(int patientId, User user, DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _patientId = patientId;
            _user = user ?? throw new ArgumentNullException(nameof(user));

            // Ustaw dane u¿ytkownika w navbarze
            UserFirstNameLabel.Text = _user.FirstName ?? "Brak danych";
            RoleLabel.Text = GetRoleName(_user.RoleID) ?? "Brak roli";

            // Inicjalizacja timera wylogowania
            InitializeLogoutTimer();

            // Dodaj gest do resetowania timera
           // AddTapGestureToResetTimer();

            // Za³aduj dane wykresu
            _ = LoadTemperatureChartDataAsync();
        }

        private string GetRoleName(int roleId)
        {
            try
            {
                var role = _databaseService.GetRoleById(roleId);
                return role?.RoleName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas pobierania nazwy roli: {ex.Message}");
                return null;
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
                await DisplayAlert("Sesja wygas³a", "Twoja sesja wygas³a z powodu braku aktywnoœci.", "OK");
                await Navigation.PopToRootAsync();
            });
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async Task LoadTemperatureChartDataAsync()
        {
            try
            {
                var temperatureLogs = await _databaseService.GetTemperatureLogsAsync(_patientId);
                var allTemperatures = temperatureLogs.OrderBy(t => t.ActionDate).ToList();
                BuildTemperatureChart(allTemperatures);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania danych do wykresu: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ wykresu temperatury.", "OK");
            }
        }

        private void BuildTemperatureChart(List<(DateTime ActionDate, decimal Temperature)> temperatureLogs)
        {
            TemperatureChartGrid.Children.Clear();
            TemperatureChartGrid.ColumnDefinitions.Clear();

            if (temperatureLogs == null || !temperatureLogs.Any())
            {
                Debug.WriteLine("Brak danych do wyœwietlenia na wykresie.");
                return;
            }

            decimal minTemperature = temperatureLogs.Min(t => t.Temperature);
            decimal maxTemperature = temperatureLogs.Max(t => t.Temperature);

            if (maxTemperature == minTemperature)
                maxTemperature = minTemperature + 1;

            foreach (var _ in temperatureLogs)
            {
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            }

            int columnIndex = 0;
            double maxChartHeight = 250;

            foreach (var log in temperatureLogs)
            {
                double verticalPosition = (double)((log.Temperature - minTemperature) / (maxTemperature - minTemperature)) * maxChartHeight;

                var point = new Ellipse
                {
                    Fill = new SolidColorBrush(Color.FromHex("#FF5733")),
                    HeightRequest = 12,
                    WidthRequest = 12,
                    Margin = new Thickness(0, maxChartHeight - verticalPosition + 20, 0, 0)
                };

                var tempLabel = new Label
                {
                    Text = $"{log.Temperature:F1}°C",
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.White,
                    FontSize = 12,
                    Margin = new Thickness(0, -20, 0, 0)
                };

                var dateLabel = new Label
                {
                    Text = log.ActionDate.ToString("dd-MM HH:mm"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.End,
                    TextColor = Colors.Gray,
                    FontSize = 10,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                TemperatureChartGrid.Children.Add(tempLabel);
                Grid.SetColumn(tempLabel, columnIndex);
                Grid.SetRow(tempLabel, 0);

                TemperatureChartGrid.Children.Add(point);
                Grid.SetColumn(point, columnIndex);
                Grid.SetRow(point, 0);

                TemperatureChartGrid.Children.Add(dateLabel);
                Grid.SetColumn(dateLabel, columnIndex);
                Grid.SetRow(dateLabel, 1);

                columnIndex += 2;
            }
        }

        //private void AddTapGestureToResetTimer()
        //{
        //    var tapGesture = new TapGestureRecognizer();
        //    tapGesture.Tapped += (_, _) => ResetLogoutTimer();
        //    this.GestureRecognizers.Add(tapGesture);
        //}
        private void OnScreenTapped(object sender, EventArgs e)
        {
            ResetLogoutTimer();
        }

        private void ResetLogoutTimer()
        {
            _remainingTimeInSeconds = LogoutTimeInSeconds;
        }
    }
}
