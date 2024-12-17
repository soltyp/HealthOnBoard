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

        public TemperatureChartPage(int patientId, DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _patientId = patientId;

            // Za³aduj dane z bazy
            _ = LoadTemperatureChartDataAsync();
        }

        private async Task LoadTemperatureChartDataAsync()
        {
            try
            {
                // Pobierz wszystkie dane o temperaturze z bazy danych
                var temperatureLogs = await _databaseService.GetTemperatureLogsAsync(_patientId);

                // Posortuj dane po dacie rosn¹co
                var allTemperatures = temperatureLogs
                                      .OrderBy(t => t.ActionDate)
                                      .ToList();

                // Przeka¿ dane do metody tworz¹cej wykres
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

            // Oblicz minimaln¹ i maksymaln¹ temperaturê
            decimal minTemperature = temperatureLogs.Min(t => t.Temperature);
            decimal maxTemperature = temperatureLogs.Max(t => t.Temperature);

            if (maxTemperature == minTemperature)
                maxTemperature = minTemperature + 1; // Zabezpieczenie przed dzieleniem przez zero

            // Dodaj kolumny dla ka¿dego punktu
            foreach (var _ in temperatureLogs)
            {
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                TemperatureChartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            }

            int columnIndex = 0;
            double maxChartHeight = 250;

            foreach (var log in temperatureLogs)
            {
                // Obliczenie wysokoœci punktu na wykresie
                double verticalPosition = (double)((log.Temperature - minTemperature) / (maxTemperature - minTemperature)) * maxChartHeight;

                // Punkt na wykresie
                var point = new Ellipse
                {
                    Fill = new SolidColorBrush(Color.FromHex("#FF5733")),
                    HeightRequest = 12,
                    WidthRequest = 12,
                    Margin = new Thickness(0, maxChartHeight - verticalPosition + 20, 0, 0)
                };

                // Etykieta temperatury nad punktem
                var tempLabel = new Label
                {
                    Text = $"{log.Temperature:F1}°C",
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.White,
                    FontSize = 12,
                    Margin = new Thickness(0, -20, 0, 0)
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

                // Dodanie elementów do siatki
                TemperatureChartGrid.Children.Add(tempLabel);
                Grid.SetColumn(tempLabel, columnIndex);
                Grid.SetRow(tempLabel, 0);

                TemperatureChartGrid.Children.Add(point);
                Grid.SetColumn(point, columnIndex);
                Grid.SetRow(point, 0);

                TemperatureChartGrid.Children.Add(dateLabel);
                Grid.SetColumn(dateLabel, columnIndex);
                Grid.SetRow(dateLabel, 1);

                columnIndex += 2; // Odstêp miêdzy punktami
            }
        }
    }
}
