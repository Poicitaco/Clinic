using ClinicManagement.Core;

namespace ClinicManagement.UI.ViewModels
{
    public class UserProfileViewModel : ViewModelBase
    {
        private Employee _currentUser;

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phoneNumber;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string FullName => _currentUser?.FullName;
        public string Role => _currentUser?.Role.ToString();

        public UserProfileViewModel()
        {
            _currentUser = UserContext.CurrentUser;
            
            if (_currentUser != null)
            {
                Email = _currentUser.Email;
                PhoneNumber = _currentUser.PhoneNumber;
            }
        }
    }
}
