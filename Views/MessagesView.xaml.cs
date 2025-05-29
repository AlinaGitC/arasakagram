using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Navigation;

namespace arasakagram.Views
{
    public partial class MessagesView : UserControl
    {
        public MessagesView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
} 