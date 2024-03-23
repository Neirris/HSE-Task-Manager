using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Task_ManagerCP3
{
    public partial class TagMakerWindow : Window
    {
        public System.Windows.Media.Color SelectedColor { get; private set; }
        public TagMakerWindow()
        {
            InitializeComponent();
            btnRemoveTag.Visibility = Visibility.Hidden;
        }

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (colorPickerTag.SelectedColor.HasValue)
            {
                SelectedColor = colorPickerTag.SelectedColor.Value;
            }
        }

        private void CancelTag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void SaveTag_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputTagName.Text))
            {
                MessageBox.Show("Введите имя тега");
                return;
            }

            if (!colorPickerTag.SelectedColor.HasValue)
            {
                MessageBox.Show("Выберите цвет тега");
                return;
            }

            var tagName = inputTagName.Text;
            var tagColor = SelectedColor.ToString();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                int userID = App.GetUserId(App.AuthToken, conn);

                string checkQuery = "SELECT COUNT(*) FROM Tags WHERE title = @title AND userID = @userID";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@title", tagName);
                    checkCmd.Parameters.AddWithValue("@userID", userID);
                    int tagExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (tagExists > 0)
                    {
                        MessageBoxResult result = MessageBox.Show("Обновить данный тег?", "Обновление тега", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            string updateQuery = "UPDATE Tags SET color = @color WHERE title = @title AND userID = @userID";
                            using (var updateCmd = new MySqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@title", tagName);
                                updateCmd.Parameters.AddWithValue("@color", tagColor);
                                updateCmd.Parameters.AddWithValue("@userID", userID);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        string query = "INSERT INTO Tags (title, color, userID) VALUES (@title, @color, @userID)";
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@title", inputTagName.Text);
                            cmd.Parameters.AddWithValue("@color", SelectedColor.ToString());
                            cmd.Parameters.AddWithValue("@userID", userID);
                            cmd.ExecuteNonQuery();
                        }

                    }

                    DialogResult = true;
                }
            }
        }

        private void InputTagName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(inputTagName.Text))
            {
                using (var conn = App.GetConnection())
                {
                    conn.Open();
                    int userID = App.GetUserId(App.AuthToken, conn);
                    string checkQuery = "SELECT COUNT(*) FROM Tags WHERE title = @title AND userID = @userID";

                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@title", inputTagName.Text.Trim());
                        checkCmd.Parameters.AddWithValue("@userID", userID);
                        int tagExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                        btnRemoveTag.Visibility = tagExists > 0 ? Visibility.Visible : Visibility.Hidden;
                    }
                }
            }
            else
            {
                btnRemoveTag.Visibility = Visibility.Hidden;
            }
        }

        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            var tagName = inputTagName.Text;
            if (!string.IsNullOrWhiteSpace(tagName))
            {
                MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранный тег?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = App.GetConnection())
                    {
                        conn.Open();
                        int userID = App.GetUserId(App.AuthToken, conn);
                        string deleteQuery = "DELETE FROM Tags WHERE title = @title AND userID = @userID";
                        using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@title", tagName);
                            cmd.Parameters.AddWithValue("@userID", userID);

                            int rowsAffected = cmd.ExecuteNonQuery();
                        }
                    }

                    inputTagName.Clear();
                    btnRemoveTag.Visibility = Visibility.Hidden;
                    DialogResult = true;
                    Close();
                }
            }
        }

    }
}
