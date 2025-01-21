using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // Import dla kszta³tów
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HospitalManagementData;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class PatientStatisticsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<BedStatisticsModel> BedStatistics { get; set; }
        public ObservableCollection<GenderStatisticsModel> GenderStatistics { get; set; }

        public PatientStatisticsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            BedStatistics = new ObservableCollection<BedStatisticsModel>();
            GenderStatistics = new ObservableCollection<GenderStatisticsModel>();
            BindingContext = this; 
            LoadStatistics();
        }

        private async void LoadStatistics()
        {
            try
            {
                await LoadBedStatistics();
                await LoadGenderStatistics();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas ³adowania danych. Szczegó³y: " + ex.Message, "OK");
            }
        }

        private async Task LoadBedStatistics()
        {
            try
            {
                // Pobierz dane ³ó¿ek
                var bedStats = await _databaseService.GetBedStatisticsAsync();
                BedStatistics.Clear();
                foreach (var bed in bedStats)
                {
                    BedStatistics.Add(bed);
                }

                // Oblicz stosunek zajêtych do wolnych ³ó¿ek
                int totalBeds = bedStats.Count;
                int occupiedBeds = bedStats.Count(b => b.PatientName != "£ó¿ko wolne");
                int freeBeds = totalBeds - occupiedBeds;

                var bedOccupancyData = new List<StatisticDataModel>
        {
            new StatisticDataModel { Label = "Zajête", Value = occupiedBeds },
            new StatisticDataModel { Label = "Wolne", Value = freeBeds }
        };

                BuildPieChartForBedOccupancy(BedOccupancyPieChartGrid, bedOccupancyData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania statystyk ³ó¿ek: {ex.Message}");
                throw;
            }
        }

        private async Task LoadGenderStatistics()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam ³adowanie statystyk p³ci...");

                // Pobierz dane p³ci pacjentów
                var genderStats = await _databaseService.GetGenderStatisticsAsync();
                Debug.WriteLine($"Pobrano {genderStats.Count} rekordów dotycz¹cych p³ci.");

                GenderStatistics.Clear();
                foreach (var gender in genderStats)
                {
                    Debug.WriteLine($"Dodawanie rekordu: {gender.Name}, {gender.Gender}");
                    GenderStatistics.Add(gender);
                }

                // Oblicz udzia³ procentowy p³ci
                int maleCount = genderStats.Count(g => g.Gender == "Mê¿czyzna");
                int femaleCount = genderStats.Count(g => g.Gender == "Kobieta");
                int unknownCount = genderStats.Count(g => g.Gender == "Nieznana"); // Obs³uga "Nieznana"

                Debug.WriteLine($"Mê¿czyŸni: {maleCount}, Kobiety: {femaleCount}, Nieznana: {unknownCount}");

                var genderData = new List<StatisticDataModel>
        {
            new StatisticDataModel { Label = "Mê¿czyŸni", Value = maleCount },
            new StatisticDataModel { Label = "Kobiety", Value = femaleCount },
            new StatisticDataModel { Label = "Nieznana", Value = unknownCount } // Dodanie "Nieznana"
        };

                BuildPieChartForBedOccupancy(GenderPieChartGrid, genderData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania statystyk p³ci: {ex.Message}");
                throw;
            }
        }



        private void BuildPieChartForBedOccupancy(Grid chartGrid, List<StatisticDataModel> data)
        {
            chartGrid.Children.Clear();

            double total = data.Sum(d => d.Value);
            double startAngle = 0;

            foreach (var item in data)
            {
                double sliceAngle = (item.Value / total) * 360;

                var pathFigure = new Microsoft.Maui.Controls.Shapes.PathFigure
                {
                    StartPoint = new Point(150, 150)
                };

                var arcPoint = GetArcPoint(150, 150, 100, startAngle + sliceAngle);

                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment { Point = GetArcPoint(150, 150, 100, startAngle) });
                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.ArcSegment
                {
                    Point = arcPoint,
                    Size = new Size(100, 100),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = sliceAngle > 180
                });
                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment { Point = new Point(150, 150) });

                var pathGeometry = new Microsoft.Maui.Controls.Shapes.PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                var slicePath = new Microsoft.Maui.Controls.Shapes.Path
                {
                    Data = pathGeometry,
                    Fill = new SolidColorBrush(GetRandomColor()),
                    Stroke = Colors.White,
                    StrokeThickness = 1
                };

                chartGrid.Children.Add(slicePath);

                double midAngle = startAngle + sliceAngle / 2;
                var labelPoint = GetArcPoint(150, 150, 120, midAngle);

                var label = new Label
                {
                    Text = $"{item.Label}: {item.Value}",
                    FontSize = 14, // Wiêksza czcionka dla lepszej czytelnoœci
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TranslationX = labelPoint.X - 150,
                    TranslationY = labelPoint.Y - 150
                };


                chartGrid.Children.Add(label);

                startAngle += sliceAngle;
            }
        }

        private Point GetArcPoint(double centerX, double centerY, double radius, double angle)
        {
            double radians = Math.PI * angle / 180.0; // Konwersja stopni na radiany
            return new Point(
                centerX + radius * Math.Cos(radians), // X = œrodek + promieñ * cos(k¹t)
                centerY + radius * Math.Sin(radians)  // Y = œrodek + promieñ * sin(k¹t)
            );
        }

        private Color GetRandomColor()
        {
            Random random = new Random();
            return Color.FromRgb(
                random.Next(0, 256), // R (od 0 do 255)
                random.Next(0, 256), // G (od 0 do 255)
                random.Next(0, 256)  // B (od 0 do 255)
            );
        }

    }


}
