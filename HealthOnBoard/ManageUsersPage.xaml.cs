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
        public ObservableCollection<Role> Roles { get; set; } // Lista ról
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
                    EditingUser.Role = _selectedRole.RoleName; // Przypisz nazwê roli
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
                Debug.WriteLine($"B³¹d podczas ³adowania u¿ytkowników: {ex.Message}");
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
                Debug.WriteLine($"B³¹d podczas ³adowania ról: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Sprawdzenie unikalnoœci PIN-u
                if (!await _databaseService.IsPinUniqueAsync(EditingUser.Pin))
                {
                    await DisplayAlert("B³¹d", "Podany PIN ju¿ istnieje. WprowadŸ inny PIN.", "OK");
                    return;
                }

                // Ustawienie domyœlnej wartoœci dla SafetyPIN (jeœli kolumna nadal istnieje)
                if (string.IsNullOrEmpty(EditingUser.SafetyPIN))
                {
                    EditingUser.SafetyPIN = "0000";
                }

                // Pobranie RoleID na podstawie wybranej roli
                var selectedRole = Roles.FirstOrDefault(r => r.RoleName == EditingUser.Role);
                if (selectedRole == null)
                {
                    await DisplayAlert("B³¹d", "Wybrana rola jest nieprawid³owa. Wybierz poprawn¹ rolê.", "OK");
                    return;
                }
                EditingUser.RoleID = selectedRole.RoleID;

                if (EditingUser.UserID == 0) // Dodawanie nowego u¿ytkownika
                {
                    await _databaseService.SaveUserAsync(EditingUser);
                    Users.Add(EditingUser);
                    await DisplayAlert("Sukces", "Nowy u¿ytkownik zosta³ dodany.", "OK");
                }
                else // Aktualizacja istniej¹cego u¿ytkownika
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
                    await DisplayAlert("Sukces", "Dane u¿ytkownika zosta³y zaktualizowane.", "OK");
                }

                // Reset pola edycji
                EditingUser = new User();
                OnPropertyChanged(nameof(EditingUser));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B³¹d podczas zapisywania u¿ytkownika: {ex.Message}");
                await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ danych u¿ytkownika.", "OK");
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
                bool confirm = await DisplayAlert("Potwierdzenie", $"Czy na pewno chcesz usun¹æ u¿ytkownika {user.FirstName}?", "Tak", "Nie");
                if (confirm)
                {
                    try
                    {
                        await _databaseService.DeleteUserAsync(user.UserID);
                        Users.Remove(user);
                        await DisplayAlert("Sukces", "U¿ytkownik zosta³ usuniêty.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"B³¹d podczas usuwania u¿ytkownika: {ex.Message}");
                        await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ u¿ytkownika.", "OK");
                    }
                }
            }
        }
    }
}
