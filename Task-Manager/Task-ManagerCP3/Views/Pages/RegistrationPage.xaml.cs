using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Task_ManagerCP3
{
    public partial class RegistrationPage : Page
    {
        private bool showLoginValidationError = true;
        private bool showPasswordValidationError = true;

        public RegistrationPage()
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

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (LoginBox.Text.Length < InputValidator.MinLoginLength || PassBox.Password.Length < InputValidator.MinPasswordLength)
            {
                MessageBox.Show("Заполните данные для регистрации!", "", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (AuthDB.Registration(LoginBox.Text, PassBox.Password))
                {
                    MessageBox.Show("Вы были успешно зарегистрированы!", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    var parentWindow = Window.GetWindow(this);
                    if (parentWindow.FindName("MainFrame") is Frame mainFrame)
                    {
                        mainFrame.Navigate(new LoginPage());
                    }
                    return;
                }
                else
                {
                    MessageBox.Show("Данный пользователь уже зарегистрирован!", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }

        }

        private void LoginReturn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow.FindName("MainFrame") is Frame mainFrame)
            {
                mainFrame.Navigate(new LoginPage());
            }
        }


    }
}
