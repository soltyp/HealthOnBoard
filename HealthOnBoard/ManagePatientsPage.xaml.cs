using Microsoft.Maui.Controls;

namespace HealthOnBoard
{
    public partial class ManagePatientsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        // Konstruktor przyjmuj¹cy DatabaseService
        public ManagePatientsPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        // Dodatkowe metody i logika dla zarz¹dzania pacjentami
    }
}
