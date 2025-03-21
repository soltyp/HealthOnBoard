using Microsoft.Maui.Controls;

namespace HealthOnBoard
{
    public partial class AdminPanelPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        // Konstruktor przyjmujący DatabaseService
        public AdminPanelPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        private async void OnManageUsersClicked(object sender, EventArgs e)
        {
            // Przejście do strony zarządzania użytkownikami z przekazaniem DatabaseService
            await Navigation.PushAsync(new ManageUsersPage(_databaseService));
        }

        private async void OnManagePatientsClicked(object sender, EventArgs e)
        {
            // Przejście do strony zarządzania pacjentami z przekazaniem DatabaseService
            await Navigation.PushAsync(new ManagePatientsPage(_databaseService));
        }
        private async void OnStatisticsButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PatientStatisticsPage(_databaseService));
        }

        private async void OnLoginAttemptsClicked(object sender, EventArgs e)
        {
            // Przejście do LoginAttemptsPage z przekazaniem DatabaseService
            await Navigation.PushAsync(new LoginAttemptsPage(_databaseService));
        }


    }
}
