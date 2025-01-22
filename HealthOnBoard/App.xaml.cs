using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using QuestPDF.Infrastructure;
namespace HealthOnBoard
{
    public partial class App : Application
    {
        public App()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            InitializeComponent();

            // Tworzenie obiektu IConfiguration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            // Przekazanie IConfiguration do LoginPage
            MainPage = new NavigationPage(new LoginPage(configuration));
        }
    }
}
