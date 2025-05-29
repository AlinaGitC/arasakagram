using arasakagram.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace arasakagram.Views
{
    /// <summary>
    /// Логика взаимодействия для RegistrationView.xaml
    /// </summary>
    public partial class RegistrationView : Window
    {
        public RegistrationView()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<RegistrationViewModel>();
            Loaded += RegistrationView_Loaded;
        }
        private void RegistrationView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegistrationViewModel vm)
            {
                vm.RegistrationSucceeded += OnRegistrationSucceeded;
            }
        }
        private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegistrationViewModel vm)
                vm.RegisterModel.Password = ((PasswordBox)sender).Password;
        }

        private void OnRegistrationSucceeded()
        {
            MessageBox.Show("Регистрация прошла успешно! Теперь войдите в систему.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoginHl_Click(object sender, RoutedEventArgs e)
        {
            var loginView = new LoginView();
            loginView.DataContext = App.ServiceProvider.GetRequiredService<LoginViewModel>();
            loginView.Show();
            this.Close();
        }
    }
}
