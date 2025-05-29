using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using arasakagram.Models;
using arasakagram.Services;
using System.Threading.Tasks;
using System;
using System.Windows.Input;

namespace arasakagram.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        public event Action LoginSucceeded;
        public event Action RegistrationSucceeded;

        public LoginModel LoginModel { get; set; } = new();
        public RegisterModel RegisterModel { get; set; } = new();

        private string errorMessage;
        public string ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }

        public ICommand LoginAsyncCommand { get; }
        public ICommand RegisterAsyncCommand { get; }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginAsyncCommand = new AsyncRelayCommand(LoginAsync);
            RegisterAsyncCommand = new AsyncRelayCommand(RegisterAsync);
        }

        public async Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            try
            {
                await _authService.LoginAsync(LoginModel);
                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;
            try
            {
                await _authService.RegisterAsync(RegisterModel);
                RegistrationSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}