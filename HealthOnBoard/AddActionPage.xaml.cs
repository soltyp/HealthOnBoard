using System.Collections.ObjectModel;

namespace HealthOnBoard
{
    public partial class AddActionPage : ContentPage
    {
        private readonly int _patientId;
        private readonly int _userId;
        private readonly DatabaseService _databaseService;
        private readonly Action _onActionAdded;

        public ObservableCollection<string> ActionTypes { get; set; } = new ObservableCollection<string>
        {
            "Dodanie wynik�w bada�",
            "Aktualizacja leczenia",
            "Podanie lek�w",
            "Dodanie komentarza"
        };

        public AddActionPage(int patientId, int userId, DatabaseService databaseService, Action onActionAdded)
        {
            InitializeComponent();
            _patientId = patientId;
            _userId = userId;
            _databaseService = databaseService;
            _onActionAdded = onActionAdded;
            BindingContext = this;
        }

        private async void OnSaveActionClicked(object sender, EventArgs e)
        {
            string selectedActionType = ActionTypePicker.SelectedItem as string;
            string actionDetails = ActionDetailsEditor.Text;

            if (string.IsNullOrWhiteSpace(selectedActionType))
            {
                await DisplayAlert("B��d", "Prosz� wybra� typ akcji.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(actionDetails))
            {
                await DisplayAlert("B��d", "Prosz� wprowadzi� szczeg�y akcji.", "OK");
                return;
            }

            bool success = await _databaseService.AddPatientActionAsync(
                userID: _userId,
                patientID: _patientId,
                actionType: selectedActionType,
                actionDetails: actionDetails,
                actionDate: DateTime.Now
            );

            if (success)
            {
                await DisplayAlert("Sukces", "Akcja zosta�a dodana pomy�lnie.", "OK");
                _onActionAdded?.Invoke();
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("B��d", "Nie uda�o si� doda� akcji.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
