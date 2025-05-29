using System.Windows;
using System.Windows.Input;

namespace arasakagram.Views
{
    public partial class InputDialog : Window
    {
        public string ResponseText => InputBox.Text;
        public string ChatName => InputBox.Text;
        public string ChatTopic => TopicBox.Text;

        public InputDialog(string prompt)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        // �����������
        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // ������������/��������������
        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        // ��������
        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ����������� ���� �� ���������
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
} 