using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Task_ManagerCP3
{
    public class InputValidator
    {
        public const string AllowedChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#%^_+&.,";
        public const string PasswordInputError = "Пароль введен некорректно!\n● Буквы: a-Z\n● Цифры: 0-9\n● Символы: !@#%^+&.,\n● Количество символов: 8-16\nМинимум 1 цифра, буква и символ";
        public const string LoginInputError = "Логин введен некорректно!\n● Буквы: a-Z\n● Цифры: 0-9\n● Символы: !@#$%^+&.,\n● Количество символов: 4-16";

        public const int MinLoginLength = 4;
        public const int MinPasswordLength = 8;
        public const int MaxUserCharsLength = 16;

        public static bool IsValidPassword(string input)
        {
            return Regex.IsMatch(input, @"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!@#$%^_+&.,])[a-zA-Z\d!@#$%^_+&.,]{8,}$");
        }

        public static bool IsValidLogin(string input)
        {
            return Regex.IsMatch(input, @"^[a-zA-Z\d!@#$%^_+&.,]{4,}$");
        }

        public static void ValidateInput(Control control, string input, Func<string, bool> isValid, string errorMessage, ref bool validationFlag)
        {
            if (validationFlag && !isValid(input))
            {
                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                control.Focus();
                validationFlag = false;
            }
        }

        public static string GetCorrectWordForm(int number, string nominativeSingular, string genitiveSingular, string genitivePlural)
        {
            number = Math.Abs(number) % 100;
            int lastDigit = number % 10;
            if (number > 10 && number < 20)
            {
                return genitivePlural;
            }
            if (lastDigit > 1 && lastDigit < 5)
            {
                return genitiveSingular;
            }
            if (lastDigit == 1)
            {
                return nominativeSingular;
            }
            return genitivePlural;
        }

        public class StringToBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var colorString = value as string;
                return (colorString != null) ? (SolidColorBrush)(new BrushConverter().ConvertFrom(colorString)) : Brushes.Transparent;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

}
