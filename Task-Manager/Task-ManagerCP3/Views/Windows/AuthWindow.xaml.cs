using System.Windows;
using System.Windows.Input;

namespace Task_ManagerCP3
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new LoginPage());
        }

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        public static AuthWindow? GetCurrentWindow()
        {
            return Application.Current.Windows
                .OfType<AuthWindow>()
                .FirstOrDefault();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MiniTray_Click(object sender, RoutedEventArgs e)
        {
            Window window = GetWindow(this);
            window.WindowState = WindowState.Minimized;
        }

    }
}
