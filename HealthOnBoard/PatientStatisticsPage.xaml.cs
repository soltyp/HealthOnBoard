using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // Import dla kszta³tów
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HospitalManagementData;
using System.Diagnostics;
using Microsoft.Maui.Controls.Internals;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.IO;

namespace HealthOnBoard
{

    public partial class PatientStatisticsPage : ContentPage
    {

        private readonly DatabaseService _databaseService;

        public ObservableCollection<BedStatisticsModel> BedStatistics { get; set; }
        public ObservableCollection<GenderStatisticsModel> GenderStatistics { get; set; }
        public ObservableCollection<BloodTypeStatisticsModel> BloodTypeStatistics { get; set; }


        public PatientStatisticsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            BedStatistics = new ObservableCollection<BedStatisticsModel>();
            GenderStatistics = new ObservableCollection<GenderStatisticsModel>();
            BloodTypeStatistics = new ObservableCollection<BloodTypeStatisticsModel>();

            BindingContext = this;
            LoadStatistics();
        }

        private async void OnExportToPdfClicked(object sender, EventArgs e)
        {
            var generator = new PdfGenerator(BedStatistics, GenderStatistics, BloodTypeStatistics);
            string filePath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "PatientStatistics.pdf");

            try
            {
                // Tworzenie PDF
                generator.CreatePdf(filePath);

                // Wyœwietlenie komunikatu z opcj¹ skopiowania
                bool copyToClipboard = await DisplayAlert("Sukces", $"PDF zapisany do: {filePath}", "Skopiuj œcie¿kê", "OK");

                // Kopiowanie œcie¿ki do schowka, jeœli u¿ytkownik wybierze "Skopiuj œcie¿kê"
                if (copyToClipboard)
                {
                    await Clipboard.Default.SetTextAsync(filePath);
                    await DisplayAlert("Skopiowano", "Œcie¿ka zosta³a skopiowana do schowka.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("B³¹d", $"Nie uda³o siê wyeksportowaæ PDF. Szczegó³y: {ex.Message}", "OK");
            }
        }



        private void BuildPieChartForBloodTypes(Grid chartGrid, List<StatisticDataModel> data)
        {
            chartGrid.Children.Clear();

            double total = data.Sum(d => d.Value);
            double startAngle = 0;
            double centerX = 200;
            double centerY = 200;
            double radius = 100;
            double labelRadius = 115; // Zmniejszono promieñ dla etykiet, aby by³y bli¿ej ko³a

            foreach (var item in data)
            {
                double sliceAngle = (item.Value / total) * 360;

                // Rysowanie jednego fragmentu wykresu
                var pathFigure = new Microsoft.Maui.Controls.Shapes.PathFigure
                {
                    StartPoint = new Point(centerX, centerY)
                };

                var startArcPoint = GetArcPoint(centerX, centerY, radius, startAngle);
                var endArcPoint = GetArcPoint(centerX, centerY, radius, startAngle + sliceAngle);

                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment { Point = startArcPoint });
                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.ArcSegment
                {
                    Point = endArcPoint,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = sliceAngle > 180
                });
                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment { Point = new Point(centerX, centerY) });

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

                // Dodanie etykiety dla fragmentu
                double midAngle = startAngle + sliceAngle / 2;
                var labelPoint = GetArcPoint(centerX, centerY, labelRadius, midAngle);

                var label = new Label
                {
                    Text = $"{item.Label}: {item.Value}",
                    FontSize = 12,
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TranslationX = labelPoint.X - centerX,
                    TranslationY = labelPoint.Y - centerY
                };

                chartGrid.Children.Add(label);

                startAngle += sliceAngle;
            }

            // Dodanie tytu³u nad wykresem
            var title = new Label
            {
                Text = "Grupy krwi pacjentów",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, -40, 0, 10)
            };

            chartGrid.Children.Add(title);//a
        }




        private async Task LoadBloodTypeStatistics()
        {
            try
            {
                // Pobierz statystyki bezpoœrednio z kolumny BloodType
                var patients = await _databaseService.GetAllPatientsAsync();
                if (patients == null || !patients.Any())
                {
                    Debug.WriteLine("Brak danych do za³adowania dla grup krwi.");
                    return;
                }

                // Grupowanie pacjentów wed³ug grupy krwi (Type w³aœciwoœæ)
                var bloodTypeData = patients
                    .Where(p => p.BloodType != null && !string.IsNullOrEmpty(p.BloodType.Type)) // Pomijamy puste wartoœci grupy krwi
                    .GroupBy(p => p.BloodType.Type)
                    .Select(g => new StatisticDataModel
                    {
                        Label = g.Key,
                        Value = g.Count()
                    })
                    .ToList();

                // Ustawienie danych dla statystyk
                // Ustawienie danych dla statystyk
                BloodTypeStatistics.Clear();
                foreach (var patient in patients.Where(p => p.BloodType != null && !string.IsNullOrEmpty(p.BloodType.Type)))
                {
                    BloodTypeStatistics.Add(new BloodTypeStatisticsModel
                    {
                        PatientName = patient.Name,
                        BedNumber = patient.BedNumber ?? -1, // Ustawiamy -1 dla "Brak ³ó¿ka"
                        BloodType = patient.BloodType.Type // Pobieramy w³aœciwoœæ Type jako string
                    });
                }


                // Budujemy wykres ko³owy z grup krwi
                BuildPieChartForBloodTypes(BloodTypePieChartGrid, bloodTypeData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania danych grup krwi: {ex.Message}");
                await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas ³adowania danych grup krwi.", "OK");
            }
        }









        private async void LoadStatistics()
        {
            try
            {
                await LoadBedStatistics();
                await LoadGenderStatistics();
                await LoadBloodTypeStatistics();
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
                // Obs³uga nieznanej p³ci (sprawdzamy tak¿e null lub puste wartoœci)
                int unknownCount = genderStats.Count(g => string.IsNullOrEmpty(g.Gender) || g.Gender == "Nieznana" || g.Gender.Trim().ToLower() == "unknown");

                // Dodanie wartoœci domyœlnej, jeœli którejœ kategorii brakuje
                if (maleCount == 0 && femaleCount == 0 && unknownCount == 0)
                {
                    Debug.WriteLine("Brak danych o p³ciach. Dodajê domyœlne wartoœci.");
                    unknownCount = 1; // Dodanie 1 dla "Nieznana" jako wartoœæ domyœlna
                }

                // Dodanie brakuj¹cych kategorii jako 0, jeœli ich brak
                if (maleCount == 0)
                {
                    Debug.WriteLine("Brak danych dla Mê¿czyzn. Dodajê wartoœæ 0.");
                }
                if (femaleCount == 0)
                {
                    Debug.WriteLine("Brak danych dla Kobiet. Dodajê wartoœæ 0.");
                }
                if (unknownCount == 0)
                {
                    Debug.WriteLine("Brak danych dla Nieznana. Dodajê wartoœæ 0.");
                }

                Debug.WriteLine($"Mê¿czyŸni: {maleCount}, Kobiety: {femaleCount}, Nieznana: {unknownCount}");

                // Przygotowanie danych do wykresu
                var genderData = new List<StatisticDataModel>
        {
            new StatisticDataModel { Label = "Mê¿czyŸni", Value = maleCount },
            new StatisticDataModel { Label = "Kobiety", Value = femaleCount },
            new StatisticDataModel { Label = "Nieznana", Value = unknownCount }
        };

                // Obs³uga przypadku, gdy brak danych do wykresu
                if (!genderData.Any(d => d.Value > 0))
                {
                    Debug.WriteLine("Brak danych do wyœwietlenia na wykresie.");
                    await DisplayAlert("Informacja", "Brak danych do wyœwietlenia statystyk p³ci.", "OK");
                    return;
                }

                // Budowanie wykresu
                BuildPieChartForBedOccupancy(GenderPieChartGrid, genderData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania statystyk p³ci: {ex.Message}");
                await DisplayAlert("B³¹d", "Wyst¹pi³ problem podczas ³adowania statystyk p³ci. Szczegó³y: " + ex.Message, "OK");
            }
        }




        private void BuildPieChartForBedOccupancy(Grid chartGrid, List<StatisticDataModel> data)
        {
            chartGrid.Children.Clear();

            double total = data.Sum(d => d.Value);

            // Nowe wartoœci œrodka dla przesuniêcia
            double centerX = 200; // Wiêcej w prawo
            double centerY = 200; // Wiêcej w dó³
            double radius = 100;
            double labelRadius = 120;

            double startAngle = 0;

            foreach (var item in data)
            {
                double sliceAngle = (item.Value / total) * 360;

                var pathFigure = new Microsoft.Maui.Controls.Shapes.PathFigure
                {
                    StartPoint = new Point(centerX, centerY)
                };

                var startArcPoint = GetArcPoint(centerX, centerY, radius, startAngle);
                var endArcPoint = GetArcPoint(centerX, centerY, radius, startAngle + sliceAngle);

                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment { Point = startArcPoint });
                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.ArcSegment
                {
                    Point = endArcPoint,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = sliceAngle > 180
                });
                pathFigure.Segments.Add(new Microsoft.Maui.Controls.Shapes.LineSegment { Point = new Point(centerX, centerY) });

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
                var labelPoint = GetArcPoint(centerX, centerY, labelRadius, midAngle);

                var label = new Label
                {
                    Text = $"{item.Label}: {item.Value}",
                    FontSize = 14, // Wiêksza czcionka dla lepszej czytelnoœci
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TranslationX = labelPoint.X - centerX,
                    TranslationY = labelPoint.Y - centerY
                };

                chartGrid.Children.Add(label);

                startAngle += sliceAngle;
            }

            // Dodanie tytu³u nad wykresem (opcjonalnie przesuniêcie tytu³u)
            var title = new Label
            {
                Text = "Stosunek zajêtych do wolnych ³ó¿ek",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, -30, 0, 10),
                TranslationX = 50 // Opcjonalne przesuniêcie tytu³u w prawo
            };

            chartGrid.Children.Add(title);
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

