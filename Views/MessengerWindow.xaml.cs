using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using arasakagram.ViewModels;

namespace arasakagram.Views
{
    public partial class MessengerWindow : Window
    {
        public MessengerWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            var vm = App.ServiceProvider.GetService(typeof(MessengerViewModel)) as MessengerViewModel;
            vm.OpenEditProfileAction = OpenEditProfile;
            DataContext = vm;
            Loaded += async (s, e) => await vm.InitCurrentUserAsync();
            SearchTxb.TextChanged += SearchTxb_TextChanged;
        }

        public void ScrollMessagesToEnd()
        {
            if (MessagesListBox.Items.Count > 0)
                MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is arasakagram.Models.ChatModel chat)
            {
                if (DataContext is arasakagram.ViewModels.MessengerViewModel vm)
                    vm.SelectedChat = chat;
            }
        }

           private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
   {
       System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
       e.Handled = true;
   }

        public void OpenEditProfile()
        {
            var vm = DataContext as arasakagram.ViewModels.MessengerViewModel;
            var profileVM = new arasakagram.ViewModels.ProfileViewModel(vm.CurrentUser, vm.LogoutCommand, avatarBytes => {
                vm.CurrentUser.Avatar = avatarBytes;
            });
            var win = new arasakagram.Views.ProfileView { DataContext = profileVM, Owner = this };
            win.ShowDialog();
        }

        private async void SearchUsersListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MessengerViewModel vm && sender is ListBox lb && lb.SelectedItem is arasakagram.ViewModels.UserInfo user)
            {
                var result = MessageBox.Show($"Начать личный чат с {user.FullNameShort}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await vm.CreateChatWithUserAsync(user);
                }
            }
        }

        private void CloseSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessengerViewModel vm)
            {
                vm.IsSearchMode = false;
                vm.SearchText = string.Empty;
            }
        }

        private void SearchTxb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MessengerViewModel vm)
            {
                if (!vm.IsSearchMode && !string.IsNullOrWhiteSpace(vm.SearchText))
                    vm.IsSearchMode = true;
            }
        }
    }
}
