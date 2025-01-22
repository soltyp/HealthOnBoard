using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes; // Import dla kszta�t�w
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

                // Wy�wietlenie komunikatu z opcj� skopiowania
                bool copyToClipboard = await DisplayAlert("Sukces", $"PDF zapisany do: {filePath}", "Skopiuj �cie�k�", "OK");

                // Kopiowanie �cie�ki do schowka, je�li u�ytkownik wybierze "Skopiuj �cie�k�"
                if (copyToClipboard)
                {
                    await Clipboard.Default.SetTextAsync(filePath);
                    await DisplayAlert("Skopiowano", "�cie�ka zosta�a skopiowana do schowka.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("B��d", $"Nie uda�o si� wyeksportowa� PDF. Szczeg�y: {ex.Message}", "OK");
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
            double labelRadius = 115; // Zmniejszono promie� dla etykiet, aby by�y bli�ej ko�a

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

            // Dodanie tytu�u nad wykresem
            var title = new Label
            {
                Text = "Grupy krwi pacjent�w",
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
                // Pobierz statystyki bezpo�rednio z kolumny BloodType
                var patients = await _databaseService.GetAllPatientsAsync();
                if (patients == null || !patients.Any())
                {
                    Debug.WriteLine("Brak danych do za�adowania dla grup krwi.");
                    return;
                }

                // Grupowanie pacjent�w wed�ug grupy krwi (Type w�a�ciwo��)
                var bloodTypeData = patients
                    .Where(p => p.BloodType != null && !string.IsNullOrEmpty(p.BloodType.Type)) // Pomijamy puste warto�ci grupy krwi
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
                        BedNumber = patient.BedNumber ?? -1, // Ustawiamy -1 dla "Brak ��ka"
                        BloodType = patient.BloodType.Type // Pobieramy w�a�ciwo�� Type jako string
                    });
                }


                // Budujemy wykres ko�owy z grup krwi
                BuildPieChartForBloodTypes(BloodTypePieChartGrid, bloodTypeData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania danych grup krwi: {ex.Message}");
                await DisplayAlert("B��d", "Wyst�pi� problem podczas �adowania danych grup krwi.", "OK");
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
                // Obs�uga nieznanej p�ci (sprawdzamy tak�e null lub puste warto�ci)
                int unknownCount = genderStats.Count(g => string.IsNullOrEmpty(g.Gender) || g.Gender == "Nieznana" || g.Gender.Trim().ToLower() == "unknown");

                // Dodanie warto�ci domy�lnej, je�li kt�rej� kategorii brakuje
                if (maleCount == 0 && femaleCount == 0 && unknownCount == 0)
                {
                    Debug.WriteLine("Brak danych o p�ciach. Dodaj� domy�lne warto�ci.");
                    unknownCount = 1; // Dodanie 1 dla "Nieznana" jako warto�� domy�lna
                }

                // Dodanie brakuj�cych kategorii jako 0, je�li ich brak
                if (maleCount == 0)
                {
                    Debug.WriteLine("Brak danych dla M�czyzn. Dodaj� warto�� 0.");
                }
                if (femaleCount == 0)
                {
                    Debug.WriteLine("Brak danych dla Kobiet. Dodaj� warto�� 0.");
                }
                if (unknownCount == 0)
                {
                    Debug.WriteLine("Brak danych dla Nieznana. Dodaj� warto�� 0.");
                }

                Debug.WriteLine($"M�czy�ni: {maleCount}, Kobiety: {femaleCount}, Nieznana: {unknownCount}");

                // Przygotowanie danych do wykresu
                var genderData = new List<StatisticDataModel>
        {
            new StatisticDataModel { Label = "M�czy�ni", Value = maleCount },
            new StatisticDataModel { Label = "Kobiety", Value = femaleCount },
            new StatisticDataModel { Label = "Nieznana", Value = unknownCount }
        };

                // Obs�uga przypadku, gdy brak danych do wykresu
                if (!genderData.Any(d => d.Value > 0))
                {
                    Debug.WriteLine("Brak danych do wy�wietlenia na wykresie.");
                    await DisplayAlert("Informacja", "Brak danych do wy�wietlenia statystyk p�ci.", "OK");
                    return;
                }

                // Budowanie wykresu
                BuildPieChartForBedOccupancy(GenderPieChartGrid, genderData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania statystyk p�ci: {ex.Message}");
                await DisplayAlert("B��d", "Wyst�pi� problem podczas �adowania statystyk p�ci. Szczeg�y: " + ex.Message, "OK");
            }
        }




        private void BuildPieChartForBedOccupancy(Grid chartGrid, List<StatisticDataModel> data)
        {
            chartGrid.Children.Clear();

            double total = data.Sum(d => d.Value);

            // Nowe warto�ci �rodka dla przesuni�cia
            double centerX = 200; // Wi�cej w prawo
            double centerY = 200; // Wi�cej w d�
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
                    FontSize = 14, // Wi�ksza czcionka dla lepszej czytelno�ci
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TranslationX = labelPoint.X - centerX,
                    TranslationY = labelPoint.Y - centerY
                };

                chartGrid.Children.Add(label);

                startAngle += sliceAngle;
            }

            // Dodanie tytu�u nad wykresem (opcjonalnie przesuni�cie tytu�u)
            var title = new Label
            {
                Text = "Stosunek zaj�tych do wolnych ��ek",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, -30, 0, 10),
                TranslationX = 50 // Opcjonalne przesuni�cie tytu�u w prawo
            };

            chartGrid.Children.Add(title);
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

