using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // Import dla kszta³tów
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HealthOnBoard
{
    public partial class PatientStatisticsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public List<StatisticDataModel> BloodTypeData { get; set; }
        public List<StatisticDataModel> AgeDistributionData { get; set; }
        public List<StatisticDataModel> BedOccupancyData { get; set; }

        public PatientStatisticsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            LoadStatistics();
        }

        private async void LoadStatistics()
        {
            var patients = await _databaseService.GetPatientsAsync();

            // Podzia³ wed³ug grupy krwi
            BloodTypeData = patients
                .GroupBy(p => p.BloodType?.Type ?? "Nieznana")
                .Select(g => new StatisticDataModel { Label = g.Key, Value = g.Count() })
                .ToList();

            // Podzia³ wed³ug wieku (grupy wiekowe)
            AgeDistributionData = patients
                .GroupBy(p => (p.Age / 10) * 10) // Grupowanie po dekadach (np. 10-19, 20-29)
                .Select(g => new StatisticDataModel { Label = $"{g.Key}-{g.Key + 9}", Value = g.Count() })
                .ToList();

            // Stosunek wolnych do zajêtych ³ó¿ek
            int totalBeds = patients.Max(p => p.BedNumber ?? 0);
            int occupiedBeds = patients.Select(p => p.BedNumber).Distinct().Count();
            int freeBeds = totalBeds - occupiedBeds;

            BedOccupancyData = new List<StatisticDataModel>
            {
                new StatisticDataModel { Label = "Zajête", Value = occupiedBeds },
                new StatisticDataModel { Label = "Wolne", Value = freeBeds }
            };

            BuildPieChartForBloodType(BloodTypePieChartGrid, BloodTypeData);
            BuildBarChartForAge(AgeBarChartGrid, AgeDistributionData);
            BuildPieChartForBedOccupancy(BedOccupancyPieChartGrid, BedOccupancyData);
        }

        private void BuildBarChartForAge(Grid chartGrid, List<StatisticDataModel> data)
        {
            chartGrid.Children.Clear();
            chartGrid.ColumnDefinitions.Clear();
            chartGrid.RowDefinitions.Clear();

            foreach (var _ in data)
            {
                chartGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            chartGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Wiersz na s³upki
            chartGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Wiersz na etykiety

            int maxCount = data.Max(d => d.Value); // ZnajdŸ maksymaln¹ wartoœæ dla skalowania wysokoœci s³upków

            for (int i = 0; i < data.Count; i++)
            {
                // Tworzenie s³upka
                var bar = new BoxView
                {
                    Color = GetRandomColor(), // Losowy kolor s³upka
                    HeightRequest = (data[i].Value / (double)maxCount) * 200, // Skaluje wysokoœæ s³upka
                    VerticalOptions = LayoutOptions.End
                };

                // Tworzenie etykiety dla osi X (np. zakres wiekowy)
                var label = new Label
                {
                    Text = data[i].Label,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                // Tworzenie etykiety z wartoœci¹ (liczb¹ pacjentów)
                var valueLabel = new Label
                {
                    Text = data[i].Value.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.End
                };

                // Dodawanie elementów do siatki
                chartGrid.Children.Add(bar);
                Grid.SetColumn(bar, i);
                Grid.SetRow(bar, 0);

                chartGrid.Children.Add(label);
                Grid.SetColumn(label, i);
                Grid.SetRow(label, 1);

                chartGrid.Children.Add(valueLabel);
                Grid.SetColumn(valueLabel, i);
                Grid.SetRow(valueLabel, 0);
            }
        }


        private void BuildPieChartForBloodType(Grid chartGrid, List<StatisticDataModel> data)
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
                    Stroke = Colors.Black,
                    StrokeThickness = 1
                };

                chartGrid.Children.Add(slicePath);

                double midAngle = startAngle + sliceAngle / 2;
                var labelPoint = GetArcPoint(150, 150, 120, midAngle);

                var label = new Label
                {
                    Text = $"{item.Label}: {item.Value}",
                    FontSize = 12,
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
                    FontSize = 12,
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
            double radians = Math.PI * angle / 180.0;
            return new Point(centerX + radius * Math.Cos(radians), centerY + radius * Math.Sin(radians));
        }

        private Color GetRandomColor()
        {
            Random random = new Random();
            return Color.FromRgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        }
    }

    public class StatisticDataModel
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }
}
