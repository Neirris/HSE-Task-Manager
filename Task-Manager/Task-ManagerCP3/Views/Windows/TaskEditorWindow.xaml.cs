using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using Task_ManagerCP3.Models;

namespace Task_ManagerCP3
{
    public partial class TaskEditorWindow : Window
    {
        private Project currentProject;
        private TaskItem currentTask;
        private bool isEditMode;
        private int currentlistID;

        public TaskEditorWindow(Project project, TaskItem taskItem = null, int listID = 0)
        {
            InitializeComponent();
            currentProject = project;
            currentTask = taskItem;
            currentlistID = listID;
            isEditMode = taskItem != null;

            SetEventHandlers();
            SetDefaultVisibility();
            if (isEditMode)
            {
                currentlistID = taskItem.TaskListID;
                LoadTags();
                LoadLists();
                LoadTaskData();
                LoadNotificationTime();
                LoadRepeatDetails();
            }
            else
            {
                LoadTags();
                LoadLists();
            }
        }


        public void SetDefaultVisibility()
        {
            reminderCheckBox.Visibility = Visibility.Hidden;
            reminderTimePicker.Visibility = Visibility.Hidden;
            repeatCheckBox.Visibility = Visibility.Hidden;
            repeatComboBox.Visibility = Visibility.Hidden;
            repeatDatePicker.Visibility = Visibility.Hidden;
            if (!isEditMode)
            {
                btnRemoveTask.Visibility = Visibility.Hidden;
            }
        }

        private void SetEventHandlers()
        {
            datePicker.ValueChanged += DatePicker_ValueChanged;
            reminderCheckBox.Checked += ReminderCheckBox_Toggled;
            reminderCheckBox.Unchecked += ReminderCheckBox_Toggled;
            repeatCheckBox.Checked += RepeatCheckBox_Toggled;
            repeatCheckBox.Unchecked += RepeatCheckBox_Toggled;
            repeatComboBox.SelectionChanged += RepeatComboBox_SelectionChanged;

        }

        /*EVENT HANDLERS*/
        private void DatePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            bool hasDate = datePicker.Value.HasValue;
            reminderCheckBox.Visibility = hasDate ? Visibility.Visible : Visibility.Hidden;
            repeatCheckBox.Visibility = hasDate ? Visibility.Visible : Visibility.Hidden;

            if (!hasDate)
            {
                reminderCheckBox.IsChecked = false;
                reminderTimePicker.Visibility = Visibility.Hidden;
                repeatCheckBox.IsChecked = false;
                repeatComboBox.Visibility = Visibility.Hidden;
                repeatDatePicker.Visibility = Visibility.Hidden;
            }
            else
            {
                reminderTimePicker.Visibility = reminderCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
                repeatComboBox.Visibility = repeatCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
            }
        }


        private void ReminderCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            reminderTimePicker.Visibility = reminderCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void RepeatCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            var isVisible = repeatCheckBox.IsChecked == true;
            repeatComboBox.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            if (!isVisible)
            {
                repeatDatePicker.Visibility = Visibility.Hidden;
                repeatDatePicker.Value = null;
            }
        }


        private void RepeatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (repeatComboBox.SelectedItem is ComboBoxItem selected)
            {
                repeatDatePicker.Visibility = selected.Content.ToString() == "Дата" ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                repeatDatePicker.Visibility = Visibility.Hidden;
                repeatDatePicker.Value = null;
            }
        }


        private void ListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                currentlistID = (int)selectedItem.Tag;
            }
        }


        private void TagsCheckComboBox_ItemSelectionChanged(object sender, Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e)
        {
            int maxCheckCount = 3;

            if (tagsCheckComboBox.SelectedItems.Count > (e.IsSelected ? maxCheckCount : maxCheckCount + 1))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    tagsCheckComboBox.SelectedItems.Remove(e.Item);
                }), System.Windows.Threading.DispatcherPriority.DataBind);
            }

            UpdateCheckComboBoxText();
        }


        /*EDIT BLOCK*/
        private void LoadTags()
        {
            List<Tag> tags = new List<Tag>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                int userID = App.GetUserId(App.AuthToken, conn);
                string commandText = "SELECT ID, Title, Color FROM Tags WHERE userID = @userID";

                using (MySqlCommand command = new MySqlCommand(commandText, conn))
                {
                    command.Parameters.AddWithValue("@userID", userID);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tags.Add(new Tag
                            {
                                ID = reader.GetInt32("ID"),
                                Title = reader.GetString("Title"),
                                Color = reader.GetString("Color")
                            });
                        }
                    }
                }
            }
            tagsCheckComboBox.ItemsSource = tags;
            if (isEditMode)
            {
                SelectTaskTags();
            }
        }

        private void SelectTaskTags()
        {
            tagsCheckComboBox.SelectedItems.Clear();

            if (currentTask.Tags != null)
            {
                foreach (var tag in currentTask.Tags)
                {
                    var matchingItem = tagsCheckComboBox.Items.Cast<Tag>().FirstOrDefault(t => t.ID == tag.ID);
                    if (matchingItem != null)
                    {
                        tagsCheckComboBox.SelectedItems.Add(matchingItem);
                    }
                }
            }
            UpdateCheckComboBoxText();
        }

        private void UpdateCheckComboBoxText()
        {
            var selectedItems = tagsCheckComboBox.SelectedItems;
            var selectedTitles = selectedItems.Cast<Tag>().Select(tag => tag.Title).ToList();

            string combinedTitles = string.Join(", ", selectedTitles);
            tagsCheckComboBox.Tag = combinedTitles;
            tagsCheckComboBox.Text = combinedTitles;
        }


        private void LoadTaskData()
        {
            titleTextBox.Text = currentTask.Title;
            notesTextBox.Text = currentTask.Notes;
            statusComboBox.SelectedValue = currentTask.Status;
            datePicker.Value = currentTask.Date;
            reminderCheckBox.IsChecked = currentTask.HasNotification;
            repeatCheckBox.IsChecked = currentTask.IsRepeat;

        }

        private void LoadNotificationTime()
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "SELECT notificationTime FROM Notification WHERE taskID = @taskID LIMIT 1";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        reminderTimePicker.Value = Convert.ToDateTime(result);
                    }
                }
            }
        }

        private void LoadRepeatDetails()
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "SELECT repeatType, repeatDateTime FROM RepeatTasks WHERE taskID = @taskID LIMIT 1";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var repeatType = reader["repeatType"].ToString();
                            var repeatDateTime = reader["repeatDateTime"] == DBNull.Value ? null : (DateTime?)reader["repeatDateTime"];

                            foreach (ComboBoxItem item in repeatComboBox.Items)
                            {
                                if (item.Content.ToString() == repeatType)
                                {
                                    repeatComboBox.SelectedItem = item;
                                    break;
                                }
                            }

                            repeatDatePicker.Value = repeatDateTime;
                        }
                    }
                }
            }
        }

        private void LoadLists()
        {
            listComboBox.SelectionChanged -= ListComboBox_SelectionChanged;
            listComboBox.Items.Clear();
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "SELECT id, title FROM TaskLists WHERE projectID = @projectID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectID", currentProject.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var listId = reader.GetInt32("id");
                            var title = reader.GetString("title");
                            var comboBoxItem = new ComboBoxItem
                            {
                                Content = title,
                                Tag = listId
                            };
                            listComboBox.Items.Add(comboBoxItem);

                            if (listId == currentlistID)
                            {
                                listComboBox.SelectedItem = comboBoxItem;
                            }
                        }
                    }
                }
                conn.Close();
            }
            listComboBox.SelectionChanged += ListComboBox_SelectionChanged;
        }

        /*BUTTONS*/

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CancelTask_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var tagMakerWindow = new TagMakerWindow();
            var result = tagMakerWindow.ShowDialog();
            if (result == true)
            {
                LoadTags();
            }

        }

        private void RemoveTask_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
            "Вы уверены, что хотите удалить выбранную задачу?",
            "",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

            if (result == MessageBoxResult.OK)
            {
                using (var conn = App.GetConnection())
                {
                    conn.Open();
                    string deleteTaskQuery = "DELETE FROM Tasks WHERE id = @taskId;";

                    using (MySqlCommand deleteTaskCmd = new MySqlCommand(deleteTaskQuery, conn))
                    {
                        deleteTaskCmd.Parameters.AddWithValue("@taskId", currentTask.Id);
                        deleteTaskCmd.ExecuteNonQuery();
                    }
                    DialogResult = true;
                    Close();

                }
            }
        }

        private async void SaveTask_Click(object sender, RoutedEventArgs e)
        {
            string title = titleTextBox.Text.Trim();
            string description = notesTextBox.Text.Trim();
            string? status = (statusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            DateTime? dateTime = datePicker.Value;
            bool isRepeat = (bool)repeatCheckBox.IsChecked;
            bool hasNotification = (bool)reminderCheckBox.IsChecked;

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название задачи!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = App.GetConnection())
            {
                conn.Open();
                int userID = App.GetUserId(App.AuthToken, conn);

                if (isEditMode)
                {
                    UpdateTask(conn, title, description, status, dateTime, isRepeat, hasNotification, userID);
                }
                else
                {
                    int taskId = CreateTask(conn, title, description, status, dateTime, isRepeat, hasNotification, userID);
                    SaveRelatedData(conn, taskId, hasNotification, isRepeat);
                }
                conn.Close();
            }
            await App.NotificationManagerInstance.RefreshNotifications();
            DialogResult = true;
            Close();
        }

        private int CreateTask(MySqlConnection conn, string title, string description, string? status, DateTime? dateTime, bool isRepeat, bool hasNotification, int userID)
        {
            string commandText = @"
                INSERT INTO Tasks (title, description, status, dateTime, isRepeat, hasNotification, taskListID, projectID, userID)
                VALUES (@title, @description, @status, @dateTime, @isRepeat, @hasNotification, @taskListID, @projectID, @userID);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand command = new MySqlCommand(commandText, conn))
            {
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@description", description);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@dateTime", dateTime.HasValue ? (object)dateTime.Value : DBNull.Value);
                command.Parameters.AddWithValue("@isRepeat", isRepeat);
                command.Parameters.AddWithValue("@hasNotification", hasNotification);
                command.Parameters.AddWithValue("@taskListID", currentlistID);
                command.Parameters.AddWithValue("@projectID", currentProject.Id);
                command.Parameters.AddWithValue("@userID", userID);

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void SaveRelatedData(MySqlConnection conn, int taskId, bool hasNotification, bool isRepeat)
        {
            if (tagsCheckComboBox.SelectedItems.Count > 0)
            {
                var selectedTags = tagsCheckComboBox.SelectedItems.Cast<Tag>().ToList();
                foreach (var tag in selectedTags)
                {
                    InsertTagSet(conn, taskId, tag.ID);
                }
            }

            if (hasNotification && reminderTimePicker.Value.HasValue)
            {
                DateTime notificationTime = reminderTimePicker.Value.Value;
                string notificationCommandText = @"
            INSERT INTO Notification (taskID, notificationTime)
            VALUES (@taskId, @notificationTime)";
                using (MySqlCommand command = new MySqlCommand(notificationCommandText, conn))
                {
                    command.Parameters.AddWithValue("@taskId", taskId);
                    command.Parameters.AddWithValue("@notificationTime", notificationTime);
                    command.ExecuteNonQuery();
                }
            }

            if (isRepeat)
            {
                string repeatType = (repeatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                DateTime? repeatDateTime = repeatDatePicker.Value;

                string repeatCommandText = @"
                    INSERT INTO RepeatTasks (taskID, repeatType, repeatDateTime)
                    VALUES (@taskId, @repeatType, @repeatDateTime)";
                using (MySqlCommand command = new MySqlCommand(repeatCommandText, conn))
                {
                    command.Parameters.AddWithValue("@taskId", taskId);
                    command.Parameters.AddWithValue("@repeatType", repeatType);
                    command.Parameters.AddWithValue("@repeatDateTime", repeatDateTime.HasValue ? (object)repeatDateTime.Value : DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void InsertTagSet(MySqlConnection connection, int taskId, int tagId)
        {
            string commandText = @"
                INSERT INTO TagsSet (taskID, tagID)
                VALUES (@taskId, @tagId);";

            using (var command = new MySqlCommand(commandText, connection))
            {
                command.Parameters.AddWithValue("@taskId", taskId);
                command.Parameters.AddWithValue("@tagId", tagId);
                command.ExecuteNonQuery();
            }
        }

        private void UpdateTask(MySqlConnection conn, string title, string description, string? status, DateTime? dateTime, bool isRepeat, bool hasNotification, int userID)
        {
            string updateTaskQuery = @"
                UPDATE Tasks
                SET title = @title, 
                    description = @description, 
                    status = @status, 
                    dateTime = @dateTime, 
                    isRepeat = @isRepeat, 
                    hasNotification = @hasNotification, 
                    taskListID = @taskListID
                WHERE id = @id";

            using (MySqlCommand updateTaskCmd = new MySqlCommand(updateTaskQuery, conn))
            {
                updateTaskCmd.Parameters.AddWithValue("@title", title);
                updateTaskCmd.Parameters.AddWithValue("@description", description);
                updateTaskCmd.Parameters.AddWithValue("@status", status);
                updateTaskCmd.Parameters.AddWithValue("@dateTime", dateTime.HasValue ? (object)dateTime.Value : DBNull.Value);
                updateTaskCmd.Parameters.AddWithValue("@isRepeat", isRepeat);
                updateTaskCmd.Parameters.AddWithValue("@hasNotification", hasNotification);
                updateTaskCmd.Parameters.AddWithValue("@taskListID", currentlistID);
                updateTaskCmd.Parameters.AddWithValue("@id", currentTask.Id);
                updateTaskCmd.ExecuteNonQuery();
            }

            string deleteTagsQuery = "DELETE FROM TagsSet WHERE taskID = @taskID";
            using (MySqlCommand deleteTagsCmd = new MySqlCommand(deleteTagsQuery, conn))
            {
                deleteTagsCmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                deleteTagsCmd.ExecuteNonQuery();
            }

            foreach (Tag tag in tagsCheckComboBox.SelectedItems)
            {
                InsertTagSet(conn, currentTask.Id, tag.ID);
            }

            if (hasNotification && reminderTimePicker.Value.HasValue)
            {
                string deleteNotificationQuery = "DELETE FROM Notification WHERE taskID = @taskID";
                using (MySqlCommand deleteNotificationCmd = new MySqlCommand(deleteNotificationQuery, conn))
                {
                    deleteNotificationCmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                    deleteNotificationCmd.ExecuteNonQuery();
                }

                DateTime notificationTime = reminderTimePicker.Value.Value;
                string insertNotificationQuery = @"
                    INSERT INTO Notification (taskID, notificationTime)
                    VALUES (@taskID, @notificationTime)";
                using (MySqlCommand insertNotificationCmd = new MySqlCommand(insertNotificationQuery, conn))
                {
                    insertNotificationCmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                    insertNotificationCmd.Parameters.AddWithValue("@notificationTime", notificationTime);
                    insertNotificationCmd.ExecuteNonQuery();
                }
            }

            if (isRepeat)
            {
                string deleteRepeatQuery = "DELETE FROM RepeatTasks WHERE taskID = @taskID";
                using (MySqlCommand deleteRepeatCmd = new MySqlCommand(deleteRepeatQuery, conn))
                {
                    deleteRepeatCmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                    deleteRepeatCmd.ExecuteNonQuery();
                }

                string repeatType = (repeatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                DateTime? repeatDateTime = repeatDatePicker.Value;
                string insertRepeatQuery = @"
                    INSERT INTO RepeatTasks (taskID, repeatType, repeatDateTime)
                    VALUES (@taskID, @repeatType, @repeatDateTime)";
                using (MySqlCommand insertRepeatCmd = new MySqlCommand(insertRepeatQuery, conn))
                {
                    insertRepeatCmd.Parameters.AddWithValue("@taskID", currentTask.Id);
                    insertRepeatCmd.Parameters.AddWithValue("@repeatType", repeatType);
                    insertRepeatCmd.Parameters.AddWithValue("@repeatDateTime", repeatDateTime.HasValue ? (object)repeatDateTime.Value : DBNull.Value);
                    insertRepeatCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
