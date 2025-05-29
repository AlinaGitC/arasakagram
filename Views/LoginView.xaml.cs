using arasakagram.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace arasakagram.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginsWindow.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<LoginViewModel>();
            Loaded += LoginView_Loaded;
        }
        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
                vm.LoginModel.Password = ((PasswordBox)sender).Password;
        }

        private void OnLoginSucceeded()
        {
            var messengerWindow = new MessengerWindow();
            messengerWindow.Show();
            this.Close();
        }

        private void RegisrtButton_Click(object sender, RoutedEventArgs e)
        {
            var regView = new RegistrationView();
            regView.DataContext = App.ServiceProvider.GetRequiredService<RegistrationViewModel>();
            regView.Show();
            this.Close();
        }
    }
}
