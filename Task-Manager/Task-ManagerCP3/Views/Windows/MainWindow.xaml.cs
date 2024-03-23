using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MySql.Data.MySqlClient;
using Hardcodet.Wpf.TaskbarNotification;
using Task_ManagerCP3.Properties;
using Task_ManagerCP3.Services;
using Task_ManagerCP3.Views.Pages;

namespace Task_ManagerCP3
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
            InitializeServices();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            SetLogo();
            StartClockTimer();
            NavigateToPage(new DashboardPage());
        }

        private void InitializeServices()
        {
            new ProjectManager(ProjectPanel, this);
            App.NotificationManagerInstance = new NotificationManager();
        }

        private void SubscribeToEvents()
        {
            if (Application.Current.Resources["NotifyIcon"] is TaskbarIcon notifyIcon)
            {
                notifyIcon.TrayLeftMouseDown += NotifyIcon_TrayLeftMouseDown;
            }
        }

        private void StartClockTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            UpdateTime();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            var timeZoneSetting = Settings.Default.TimeZone;
            var timeZone = !string.IsNullOrWhiteSpace(timeZoneSetting)
                ? TimeZoneInfo.FindSystemTimeZoneById(timeZoneSetting)
                : TimeZoneInfo.Local;

            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            logo_time.Text = currentTime.ToString("HH:mm");
        }

        private void SetLogo()
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand())
                {
                    int user_id = 0;
                    string user_login = "";
                    cmd.Connection = conn;
                    cmd.CommandText = @"SELECT id, username FROM users WHERE token = @token";
                    cmd.Parameters.AddWithValue("@token", App.AuthToken);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user_id = (int)reader.GetInt64(0);
                            user_login = reader.GetString(1);
                        }
                    }

                    cmd.CommandText = @"SELECT profilePicture FROM UsersProfile WHERE userID = @userID";
                    cmd.Parameters.AddWithValue("@userID", user_id);
                    object result = cmd.ExecuteScalar();
                    string user_pfp = result != DBNull.Value ? (string)result
                        : "https://cdn.discordapp.com/attachments/1084994025028866149/1088999354414669834/image.png?ex=65fe31ae&is=65ebbcae&hm=2641ba6ad60c2c0cb6e461d3311f89227299a9f6ec4f10091973176fc72a8fb7&";

                    logo_pfp.Source = new BitmapImage(new Uri(user_pfp));
                    logo_username.Text = user_login;

                }
            }
        }

        private void NavigateToPage(Page page)
        {
            if (GetWindow(this)?.FindName("MainFrame") is Frame mainFrame)
            {
                mainFrame.Navigate(page);
            }
        }

        private void NotifyIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ShowFromTray();
        }

        private void ShowFromTray()
        {
            GetCurrentWindow()?.Show();
            WindowState = WindowState.Normal;
            ((TaskbarIcon)Application.Current.Resources["NotifyIcon"]).Visibility = Visibility.Collapsed;
        }

        private void HideToTray()
        {
            Hide();
            ((TaskbarIcon)Application.Current.Resources["NotifyIcon"]).Visibility = Visibility.Visible;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MiniTray_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        public static MainWindow GetCurrentWindow()
        {
            return Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        }

        private void Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(new SettingsPage());
        }

        private void Dashboard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(new DashboardPage());
        }

        private void Dashboard_MouseEnter(object sender, MouseEventArgs e)
        {
            SetButtonHoverForeground(btnDashboard);
        }

        private void Dashboard_MouseLeave(object sender, MouseEventArgs e)
        {
            SetButtonNormalForeground(btnDashboard);
        }

        private void Settings_MouseEnter(object sender, MouseEventArgs e)
        {
            SetButtonHoverForeground(btnSettings);
        }

        private void Settings_MouseLeave(object sender, MouseEventArgs e)
        {
            SetButtonNormalForeground(btnSettings);
        }

        private void SetButtonHoverForeground(TextBlock button)
        {
            button.Foreground = new BrushConverter().ConvertFromString("#9da1a4") as SolidColorBrush;
        }

        private void SetButtonNormalForeground(TextBlock button)
        {
            button.Foreground = new SolidColorBrush(Colors.White);
        }

        public void UpdateProfilePicture(Uri imageUri)
        {
            logo_pfp.Source = new BitmapImage(imageUri);
        }

        private void Notifications_Loaded(object sender, RoutedEventArgs e)
        {
            App.NotificationManagerInstance = new NotificationManager();
        }
    }
}