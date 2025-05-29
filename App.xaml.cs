using System.Configuration;
using System.Data;
using System.Windows;
using arasakagram.Api;
using arasakagram.Services;
using arasakagram.ViewModels;
using arasakagram.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace arasakagram
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            services.AddSingleton(new HttpClient());
            services.AddSingleton(provider => new Client("http://localhost:5047/", provider.GetRequiredService<HttpClient>()));
            services.AddSingleton<AuthService>();
            services.AddSingleton<ChatService>();
            services.AddSingleton<GroupService>();
            services.AddSingleton<UserService>();
            services.AddSingleton<SignalRService>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegistrationViewModel>();
            services.AddTransient<RegistrationView>();
            services.AddTransient<MessengerViewModel>(provider =>
                new MessengerViewModel(
                    provider.GetRequiredService<Client>(),
                    provider.GetRequiredService<ChatService>(),
                    provider.GetRequiredService<GroupService>(),
                    provider.GetRequiredService<UserService>(),
                    provider.GetRequiredService<SignalRService>()
                ));

            ServiceProvider = services.BuildServiceProvider();

            var loginView = new LoginView
            {
                DataContext = ServiceProvider.GetRequiredService<LoginViewModel>()
            };
            loginView.Show();

            base.OnStartup(e);
        }


    }
}
