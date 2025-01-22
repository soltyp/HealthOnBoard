using HospitalManagementData;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Linq;
using MauiColors = Microsoft.Maui.Graphics.Colors;
using PdfColors = QuestPDF.Helpers.Colors;

namespace HealthOnBoard
{
    public class PdfGenerator
    {
        private readonly ObservableCollection<BedStatisticsModel> _bedStatistics;
        private readonly ObservableCollection<GenderStatisticsModel> _genderStatistics;
        private readonly ObservableCollection<BloodTypeStatisticsModel> _bloodTypeStatistics;

        public PdfGenerator(
            ObservableCollection<BedStatisticsModel> bedStatistics,
            ObservableCollection<GenderStatisticsModel> genderStatistics,
            ObservableCollection<BloodTypeStatisticsModel> bloodTypeStatistics)
        {
            _bedStatistics = bedStatistics;
            _genderStatistics = genderStatistics;
            _bloodTypeStatistics = bloodTypeStatistics;
        }

        public void CreatePdf(string filePath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(PdfColors.White); // Użycie aliasu dla QuestPDF
                    page.DefaultTextStyle(x => x.FontSize(12).FontColor(PdfColors.Black)); // Alias

                    // Nagłówek
                    page.Header()
                        .AlignCenter()
                        .Text("Statystyki Pacjentów")
                        .SemiBold().FontSize(24).FontColor(PdfColors.Blue.Darken2); // Alias

                    // Zawartość
                    page.Content().Column(column =>
                    {
                        column.Spacing(20);

                        // Sekcja statystyk łóżek
                        column.Item().Text("Statystyki Łóżek").SemiBold().FontSize(18);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Kolumna Łóżka
                                columns.RelativeColumn(); // Kolumna Pacjenta
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Łóżko").Bold();
                                header.Cell().Text("Pacjent").Bold();
                            });

                            foreach (var bed in _bedStatistics)
                            {
                                table.Cell().Text(bed.BedNumber.ToString());
                                table.Cell().Text(bed.PatientName);
                            }
                        });

                        // Sekcja statystyk płci
                        column.Item().Text("Statystyki Płci").SemiBold().FontSize(18);
                        var maleCount = _genderStatistics.Count(g => g.Gender == "Mężczyzna");
                        var femaleCount = _genderStatistics.Count(g => g.Gender == "Kobieta");
                        var unknownCount = _genderStatistics.Count(g => g.Gender == "Nieznana");

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Mężczyźni: {maleCount}");
                            row.RelativeItem().Text($"Kobiety: {femaleCount}");
                            row.RelativeItem().Text($"Nieznana: {unknownCount}");
                        });

                        // Sekcja statystyk grup krwi
                        column.Item().Text("Statystyki Grupy Krwi").SemiBold().FontSize(18);
                        var bloodTypeGroups = _bloodTypeStatistics
                            .GroupBy(b => b.BloodType)
                            .Select(g => new { BloodType = g.Key, Count = g.Count() });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Kolumna Grupa Krwi
                                columns.RelativeColumn(); // Kolumna Liczba Pacjentów
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Grupa Krwi").Bold();
                                header.Cell().Text("Liczba Pacjentów").Bold();
                            });

                            foreach (var group in bloodTypeGroups)
                            {
                                table.Cell().Text(group.BloodType);
                                table.Cell().Text(group.Count.ToString());
                            }
                        });
                    });

                    // Stopka
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Strona ");
                            x.CurrentPageNumber();
                            x.Span(" z ");
                            x.TotalPages();
                        });
                });
            });

            document.GeneratePdf(filePath);
        }
    }
}
