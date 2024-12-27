using HospitalManagementData;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HealthOnBoard
{
    public partial class ManageUsersPage : ContentPage
    {
        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<Role> Roles { get; set; } // Lista r�l
        public User EditingUser { get; set; }

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
                    EditingUser.Role = _selectedRole.RoleName; // Przypisz nazw� roli
                    EditingUser.RoleID = _selectedRole.RoleID; // Przypisz ID roli
                }
                OnPropertyChanged();
            }
        }

        public ManageUsersPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;

            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();
            EditingUser = new User();

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUsersAsync();
            await LoadRolesAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _databaseService.GetUsersAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania u�ytkownik�w: {ex.Message}");
            }
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                var roles = await _databaseService.GetRolesAsync(); // Pobierz role z bazy
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas �adowania r�l: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Sprawdzenie unikalno�ci PIN-u
                if (!await _databaseService.IsPinUniqueAsync(EditingUser.Pin))
                {
                    await DisplayAlert("B��d", "Podany PIN ju� istnieje. Wprowad� inny PIN.", "OK");
                    return;
                }

                // Ustawienie domy�lnej warto�ci dla SafetyPIN (je�li kolumna nadal istnieje)
                if (string.IsNullOrEmpty(EditingUser.SafetyPIN))
                {
                    EditingUser.SafetyPIN = "0000";
                }

                // Pobranie RoleID na podstawie wybranej roli
                var selectedRole = Roles.FirstOrDefault(r => r.RoleName == EditingUser.Role);
                if (selectedRole == null)
                {
                    await DisplayAlert("B��d", "Wybrana rola jest nieprawid�owa. Wybierz poprawn� rol�.", "OK");
                    return;
                }
                EditingUser.RoleID = selectedRole.RoleID;

                if (EditingUser.UserID == 0) // Dodawanie nowego u�ytkownika
                {
                    await _databaseService.SaveUserAsync(EditingUser);
                    Users.Add(EditingUser);
                    await DisplayAlert("Sukces", "Nowy u�ytkownik zosta� dodany.", "OK");
                }
                else // Aktualizacja istniej�cego u�ytkownika
                {
                    await _databaseService.SaveUserAsync(EditingUser);
                    var user = Users.FirstOrDefault(u => u.UserID == EditingUser.UserID);
                    if (user != null)
                    {
                        user.FirstName = EditingUser.FirstName;
                        user.Role = EditingUser.Role;
                        user.RoleID = EditingUser.RoleID; // Aktualizacja RoleID
                        user.ActiveStatus = EditingUser.ActiveStatus;
                        user.Pin = EditingUser.Pin;
                    }
                    await DisplayAlert("Sukces", "Dane u�ytkownika zosta�y zaktualizowane.", "OK");
                }

                // Reset pola edycji
                EditingUser = new User();
                OnPropertyChanged(nameof(EditingUser));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��d podczas zapisywania u�ytkownika: {ex.Message}");
                await DisplayAlert("B��d", "Nie uda�o si� zapisa� danych u�ytkownika.", "OK");
            }
        }




        private void OnCancelClicked(object sender, EventArgs e)
        {
            EditingUser = new User();
            OnPropertyChanged(nameof(EditingUser));
        }

        private async void OnEditUserClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is User user)
            {
                await Navigation.PushAsync(new EditUserPage(user.UserID, _databaseService));
            }
        }



        private async void OnDeleteUserClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is User user)
            {
                bool confirm = await DisplayAlert("Potwierdzenie", $"Czy na pewno chcesz usun�� u�ytkownika {user.FirstName}?", "Tak", "Nie");
                if (confirm)
                {
                    try
                    {
                        await _databaseService.DeleteUserAsync(user.UserID);
                        Users.Remove(user);
                        await DisplayAlert("Sukces", "U�ytkownik zosta� usuni�ty.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B��d podczas usuwania u�ytkownika: {ex.Message}");
                        await DisplayAlert("B��d", "Nie uda�o si� usun�� u�ytkownika.", "OK");
                    }
                }
            }
        }
    }
}
