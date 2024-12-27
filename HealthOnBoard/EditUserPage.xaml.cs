using HospitalManagementData;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthOnBoard
{
    public partial class EditUserPage : ContentPage
    {
        public User SelectedUser { get; set; }
        public ObservableCollection<Role> Roles { get; set; }
        private readonly DatabaseService _databaseService;

        private Role _selectedRole;
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                if (_selectedRole != null)
                {
                    SelectedUser.Role = _selectedRole.RoleName; // Aktualizacja nazwy roli
                    SelectedUser.RoleID = _selectedRole.RoleID; // Aktualizacja ID roli
                }
                OnPropertyChanged();
            }
        }

        public EditUserPage(int userId, DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            SelectedUser = new User();
            Roles = new ObservableCollection<Role>();
            BindingContext = this;

            LoadUser(userId);
        }

        private async void LoadUser(int userId)
        {
            try
            {
                // Pobierz dane u¿ytkownika z bazy danych
                var user = await _databaseService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    SelectedUser = user;

                    // Zaktualizuj powi¹zania
                    OnPropertyChanged(nameof(SelectedUser));

                    // Ustaw odpowiedni¹ rolê
                    SelectedRole = Roles.FirstOrDefault(role => role.RoleID == SelectedUser.RoleID);
                }
                else
                {
                    Debug.WriteLine("Nie znaleziono u¿ytkownika.");
                }
                Debug.WriteLine($"Imiê i nazwisko: {SelectedUser.FirstName}");
                Debug.WriteLine($"PIN: {SelectedUser.Pin}");
                Debug.WriteLine($"RoleID: {SelectedUser.RoleID}");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania u¿ytkownika: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê za³adowaæ danych u¿ytkownika.", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRolesAsync();

            // Ustaw rolê, jeœli jeszcze nie zosta³a ustawiona
            SelectedRole = Roles.FirstOrDefault(role => role.RoleID == SelectedUser.RoleID);
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                var roles = await _databaseService.GetRolesAsync();
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas ³adowania ról: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Sprawdzenie unikalnoœci PIN-u dla edytowanego u¿ytkownika
                bool isPinUnique = await _databaseService.IsPinUniqueAsync(SelectedUser.Pin, SelectedUser.UserID);
                if (!isPinUnique)
                {
                    await DisplayAlert("B³¹d", "Podany PIN ju¿ istnieje i nale¿y do innego u¿ytkownika. WprowadŸ inny PIN.", "OK");
                    return;
                }

                // Przypisanie domyœlnej wartoœci dla SafetyPIN
                SelectedUser.SafetyPIN ??= "0000";

                // Aktualizacja danych u¿ytkownika w bazie danych
                await _databaseService.SaveUserAsync(SelectedUser);

                await DisplayAlert("Sukces", "Dane u¿ytkownika zosta³y zapisane.", "OK");
                await Navigation.PopAsync(); // Powrót do ManageUsersPage
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas zapisywania u¿ytkownika: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ danych u¿ytkownika.", "OK");
            }
        }




        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Powrót do ManageUsersPage
        }
    }
}
