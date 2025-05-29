using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using arasakagram.Services;
using System.Windows;
using System.Windows.Input;
using arasakagram.Api;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace arasakagram.ViewModels
{
    public class ProfileViewModel : ObservableObject
    {
        private UserInfo currentUser;
        public UserInfo CurrentUser
        {
            get => currentUser;
            set => SetProperty(ref currentUser, value);
        }

        public IAsyncRelayCommand SaveProfileCommand { get; }
        public ICommand LogoutCommand { get; }
        public IAsyncRelayCommand ChangeAvatarCommand { get; }

        private readonly Client _apiClient;
        private readonly System.Action<byte[]>? _onAvatarChanged;

        public ProfileViewModel(UserInfo user, ICommand logoutCommand, System.Action<byte[]>? onAvatarChanged = null)
        {
            CurrentUser = user;
            SaveProfileCommand = new AsyncRelayCommand(SaveProfileAsync);
            LogoutCommand = logoutCommand;
            ChangeAvatarCommand = new AsyncRelayCommand(ChangeAvatarAsync);
            _apiClient = App.ServiceProvider.GetService(typeof(Client)) as Client;
            _onAvatarChanged = onAvatarChanged;
        }

        private async Task ChangeAvatarAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                await _apiClient.AvatarPUTAsync(CurrentUser.Id, new AvatarDto { Avatar = bytes });
                CurrentUser.Avatar = bytes;
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(CurrentUser.AvatarImage));
                _onAvatarChanged?.Invoke(bytes);
            }
        }

        private async Task SaveProfileAsync()
        {
            var dto = new UserDto
            {
                Id = CurrentUser.Id,
                Login = CurrentUser.Login,
                Firstname = CurrentUser.FirstName,
                LastName = CurrentUser.LastName,
                MiddleName = CurrentUser.MiddleName,
                Phone = CurrentUser.Phone,
                Email = CurrentUser.Email,
                Avatar = CurrentUser.Avatar
            };
            await _apiClient.UserAsync(CurrentUser.Id, dto);
            MessageBox.Show("Изменения профиля сохранены!", "Профиль");
        }
    }
} 