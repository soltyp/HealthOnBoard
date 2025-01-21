using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // Import dla kszta�t�w
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
                Debug.WriteLine($"B��d: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                await DisplayAlert("B��d", "Wyst�pi� problem podczas �adowania danych. Szczeg�y: " + ex.Message, "OK");
            }
        }

        private async Task LoadBedStatistics()
        {
            try
            {
                // Pobierz dane ��ek
                var bedStats = await _databaseService.GetBedStatisticsAsync();
                BedStatistics.Clear();
                foreach (var bed in bedStats)
                {
                    BedStatistics.Add(bed);
                }

                // Oblicz stosunek zaj�tych do wolnych ��ek
                int totalBeds = bedStats.Count;
                int occupiedBeds = bedStats.Count(b => b.PatientName != "��ko wolne");
                int freeBeds = totalBeds - occupiedBeds;

                var bedOccupancyData = new List<StatisticDataModel>
        {
            new StatisticDataModel { Label = "Zaj�te", Value = occupiedBeds },
            new StatisticDataModel { Label = "Wolne", Value = freeBeds }
        };

                BuildPieChartForBedOccupancy(BedOccupancyPieChartGrid, bedOccupancyData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania statystyk ��ek: {ex.Message}");
                throw;
            }
        }

        private async Task LoadGenderStatistics()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam �adowanie statystyk p�ci...");

                // Pobierz dane p�ci pacjent�w
                var genderStats = await _databaseService.GetGenderStatisticsAsync();
                Debug.WriteLine($"Pobrano {genderStats.Count} rekord�w dotycz�cych p�ci.");

                GenderStatistics.Clear();
                foreach (var gender in genderStats)
                {
                    Debug.WriteLine($"Dodawanie rekordu: {gender.Name}, {gender.Gender}");
                    GenderStatistics.Add(gender);
                }

                // Oblicz udzia� procentowy p�ci
                int maleCount = genderStats.Count(g => g.Gender == "M�czyzna");
                int femaleCount = genderStats.Count(g => g.Gender == "Kobieta");
                int unknownCount = genderStats.Count(g => g.Gender == "Nieznana"); // Obs�uga "Nieznana"

                Debug.WriteLine($"M�czy�ni: {maleCount}, Kobiety: {femaleCount}, Nieznana: {unknownCount}");

                var genderData = new List<StatisticDataModel>
        {
            new StatisticDataModel { Label = "M�czy�ni", Value = maleCount },
            new StatisticDataModel { Label = "Kobiety", Value = femaleCount },
            new StatisticDataModel { Label = "Nieznana", Value = unknownCount } // Dodanie "Nieznana"
        };

                BuildPieChartForBedOccupancy(GenderPieChartGrid, genderData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania statystyk p�ci: {ex.Message}");
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
                    FontSize = 14, // Wi�ksza czcionka dla lepszej czytelno�ci
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
                centerX + radius * Math.Cos(radians), // X = �rodek + promie� * cos(k�t)
                centerY + radius * Math.Sin(radians)  // Y = �rodek + promie� * sin(k�t)
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
