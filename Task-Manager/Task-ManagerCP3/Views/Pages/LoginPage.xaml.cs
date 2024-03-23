using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Task_ManagerCP3.Properties;

namespace Task_ManagerCP3
{
    public partial class LoginPage : Page
    {
        private bool showLoginValidationError = true;
        private bool showPasswordValidationError = true;

        public LoginPage()
        {
            InitializeComponent();
            PassBox.MaxLength = InputValidator.MaxUserCharsLength;
            LoginBox.MaxLength = InputValidator.MaxUserCharsLength;
            PassBox.PreviewTextInput += (sender, e) => PreviewTextInput(e, InputValidator.AllowedChars);
            LoginBox.PreviewTextInput += (sender, e) => PreviewTextInput(e, InputValidator.AllowedChars);
        }

        private new static void PreviewTextInput(TextCompositionEventArgs e, string allowedChars)
        {
            e.Handled = e.Text.Any(c => !allowedChars.Contains(c));
        }

        private void PassBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            showPasswordValidationError = true;
            PassHint.Visibility = PassBox.Password.Length == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void LoginBox_TextChanged(object sender, RoutedEventArgs e)
        {
            showLoginValidationError = true;
        }

        private void PassBox_LostFocus(object sender, RoutedEventArgs e)
        {
            InputValidator.ValidateInput(PassBox, PassBox.Password, InputValidator.IsValidPassword, InputValidator.PasswordInputError, ref showPasswordValidationError);
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            InputValidator.ValidateInput(LoginBox, LoginBox.Text, InputValidator.IsValidLogin, InputValidator.LoginInputError, ref showLoginValidationError);
        }

        private void Auth_Click(object sender, RoutedEventArgs e)
        {
            if (LoginBox.Text.Length < InputValidator.MinLoginLength || PassBox.Password.Length < InputValidator.MinPasswordLength)
            {
                MessageBox.Show("Заполните данные для входа!", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!InputValidator.IsValidLogin(LoginBox.Text) || !InputValidator.IsValidPassword(PassBox.Password))
            {
                MessageBox.Show("Проверьте логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                showLoginValidationError = true;
                showPasswordValidationError = true;
                return;
            }

            else
            {
                var auth = AuthDB.Authentication(LoginBox.Text, PassBox.Password);
                if (auth.IsUserExists == false)
                {
                    MessageBox.Show("Данного пользователя не существует!", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else if (auth.IsPasswordCorrect == false)
                {
                    MessageBox.Show("Проверьте логин или пароль!", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else if (auth.IsUserExists == true && auth.IsPasswordCorrect == true)
                {
                    if (cbRememberUserData.IsChecked == true)
                    {
                        Settings.Default.Token = App.AuthToken;
                        Settings.Default.RememberMe = true;
                        Settings.Default.Save();
                    }
                    else
                    {
                        Settings.Default.Token = string.Empty;
                        Settings.Default.RememberMe = false;
                        Settings.Default.Save();
                    }

                    MainWindow mainWindow = new MainWindow();
                    AuthWindow.GetCurrentWindow()?.Close();
                    mainWindow.Show();
                }
            }
     
        }


        private void RegisterRedirect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow.FindName("MainFrame") is Frame mainFrame)
            {
                mainFrame.Navigate(new RegistrationPage());
            }
        }

        private void UserInfo_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.RememberMe && Settings.Default.Token != null)
            {
                bool isTokenExists = AuthDB.TokenCheck(Settings.Default.Token);
                if (isTokenExists)
                {
                    App.AuthToken = Settings.Default.Token;
                    MainWindow mainWindow = new MainWindow();
                    AuthWindow.GetCurrentWindow()?.Close();
                    mainWindow.Show();
                }
            }
        }
    }
}
