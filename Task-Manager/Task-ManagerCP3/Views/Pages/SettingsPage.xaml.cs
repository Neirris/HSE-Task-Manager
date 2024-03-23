using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Task_ManagerCP3.Properties;

namespace Task_ManagerCP3
{
    public partial class SettingsPage : Page
    {
        private TextBox textBoxForImageUrl;
        private TextBlock textBlockImageUrl;

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();

            textBlockImageUrl = change_pfp;
            textBoxForImageUrl = new TextBox
            {
                Width = 272,
                Height = 30,
                FontSize = 22,
                Foreground = new SolidColorBrush(Colors.Black),
                Margin = new Thickness(5, 5, 0, 0),
            };
            textBoxForImageUrl.KeyDown += TextBoxForImageUrl_KeyDown;


        }

        private void LoadSettings()
        {
            notificationCheckBox.IsChecked = Settings.Default.IsNotificationEnabled;
            notificationSoundCheckBox.IsChecked = Settings.Default.IsNotificationSoundEnabled;
            PfpSet();
        }


        private void PfpSet()
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText = @"SELECT profilePicture FROM UsersProfile WHERE userID = @userID";
                    cmd.Parameters.AddWithValue("@userID", App.GetUserId(App.AuthToken, conn));
                    object result = cmd.ExecuteScalar();
                    string user_pfp = result != DBNull.Value ? (string)result
                        : "https://cdn.discordapp.com/attachments/1084994025028866149/1088999354414669834/image.png?ex=65fe31ae&is=65ebbcae&hm=2641ba6ad60c2c0cb6e461d3311f89227299a9f6ec4f10091973176fc72a8fb7&";
                    logo_pfp.Source = new BitmapImage(new Uri(user_pfp));

                }
            }

        }

        private void ChangePfp_MouseEnter(object sender, MouseEventArgs e)
        {
            change_pfp.Foreground = new BrushConverter().ConvertFromString("#9da1a4") as SolidColorBrush;
        }

        private void ChangePfp_MouseLeave(object sender, MouseEventArgs e)
        {
            change_pfp.Foreground = new SolidColorBrush(Colors.White);
        }


        private void ChangePfp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            textBlockImageUrl.Visibility = Visibility.Hidden;
            var stackPanel = (StackPanel)textBlockImageUrl.Parent;
            int index = stackPanel.Children.IndexOf(textBlockImageUrl);
            stackPanel.Children.Insert(index, textBoxForImageUrl);
            textBoxForImageUrl.Focus();
        }


        private void TextBoxForImageUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var imageUrl = textBoxForImageUrl.Text;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    bool? isUrlValid = App.IsImageUrl(imageUrl);
                    if (isUrlValid.HasValue && isUrlValid.Value)
                    {
                        try
                        {
                            logo_pfp.Source = new BitmapImage(new Uri(imageUrl));
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Ошибка: Невозможно загрузить изображение");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка: Неправильный адрес изображения");
                    }
                    
                }

                var stackPanel = (StackPanel)textBlockImageUrl.Parent;
                stackPanel.Children.Remove(textBoxForImageUrl);
                textBlockImageUrl.Visibility = Visibility.Visible;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var imageUrl = textBoxForImageUrl.Text;
            Settings.Default.IsNotificationEnabled = (bool)notificationCheckBox.IsChecked;
            Settings.Default.IsNotificationSoundEnabled = (bool)notificationSoundCheckBox.IsChecked;
            Settings.Default.Save();

            if (!string.IsNullOrEmpty(imageUrl))
            {
                UpdateUserProfilePicture(imageUrl);
            }
            MessageBoxResult result = MessageBox.Show(
               "Настройки успешно обновлены", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите выйти из аккаунта?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Settings.Default.Token = string.Empty;
                Settings.Default.RememberMe = false;
                Settings.Default.Save();
                new AuthWindow().Show();
                MainWindow.GetCurrentWindow().Close();
            }
        }


        private void UpdateUserProfilePicture(string imageUrl)
        {
            try
            {
                using (MySqlConnection conn = App.GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE UsersProfile SET profilePicture = @profilePicture WHERE userID = @userID";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@profilePicture", imageUrl);
                        cmd.Parameters.AddWithValue("@userID", App.GetUserId(App.AuthToken, conn));
                        cmd.ExecuteNonQuery();
                    }
                }
                var mainWindow = MainWindow.GetCurrentWindow();
                mainWindow?.UpdateProfilePicture(new Uri(imageUrl));
                MessageBox.Show("Настройки обновлены!");
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка при обновлении изображения: ");
            }
        }

    }
}
