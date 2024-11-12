using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
namespace HealthOnBoard
{
    public partial class App : Application
    {
        public App()
        {
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
