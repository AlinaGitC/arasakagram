using arasakagram.Api;
using arasakagram.Models;
using System.Threading.Tasks;
using System.Diagnostics;

namespace arasakagram.Services
{
    public class AuthService
    {
        private readonly Api.Client _userApi;
        public static int? CurrentUserId { get; private set; }

        public AuthService(Api.Client userApi)
        {
            _userApi = userApi;
        }

        public async Task LoginAsync(LoginModel model)
        {
            ErrorLog($"[LOGIN] Попытка входа: {model.Login}");
            try
            {
                var result = await _userApi.LoginAsync(new LoginUserDto
                {
                    Login = model.Login,
                    Password = model.Password
                });
                CurrentUserId = result?.Id;
                ErrorLog($"[LOGIN] Успешно. UserId: {CurrentUserId}");
            }
            catch (Exception ex)
            {
                ErrorLog($"[LOGIN][ОШИБКА] {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task RegisterAsync(RegisterModel model)
        {
            await _userApi.RegisterAsync(new RegisterUserDto
            {
                Login = model.Login,
                Password = model.Password,
                Firstname = model.Firstname,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                MiddleName = model.MiddleName
            });
        }

        public static void Logout()
        {
            CurrentUserId = null;
        }

        private void ErrorLog(string msg)
        {
            Debug.WriteLine(msg);
            System.IO.File.AppendAllText("login_debug.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}\n");
        }
    }
}