using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using Task_ManagerCP3.Models;

namespace Task_ManagerCP3
{
    public partial class TaskManagerPage : Page
    {
        private Project currentProject;

        public TaskManagerPage(Project project)
        {
            InitializeComponent();
            currentProject = project;
            LoadTaskLists();
        }



        private void LoadTaskLists()
        {
            var taskLists = GetTaskListsForProject(currentProject.Id);
            bool isFirst = true;

            foreach (var taskList in taskLists)
            {
                var expander = new Expander
                {
                    IsExpanded = true,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var headerTextBlock = new TextBlock
                {
                    Text = taskList.Title,
                    FontSize = 22,
                    Foreground = Brushes.White
                };
                expander.Header = headerTextBlock;

                var contentStackPanel = new StackPanel();

                var listView = CreateTaskListView(taskList, isFirst);
                contentStackPanel.Children.Add(listView);

                if (GetTaskCountForList(taskList.Id) < 10)
                {
                    var addButton = CreateAddTaskButton(taskList.Id);
                    contentStackPanel.Children.Add(addButton);
                }

                var deleteButton = CreateDeleteButton(taskList.Id);
                expander.Content = new Grid
                {
                    Children =
                    {
                        contentStackPanel,
                        deleteButton
                    }
                };

                TaskListPanel.Children.Add(expander);

                isFirst = false;
            }
        }

        private Button CreateDeleteButton(int taskListId)
        {
            var button = new Button
            {
                Content = "X",
                Width = 30,
                Height = 30,
                FontSize = 18,
                Background = new BrushConverter().ConvertFrom("#D94044") as SolidColorBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 10, 0, 0)
            };
            button.Click += (sender, e) =>
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот список задач?", "", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    DeleteTaskList(taskListId);
                }
            };
            return button;
        }

        private Button CreateAddTaskButton(int taskListId)
        {
            var button = new Button
            {
                Content = "+",
                Width = 30,
                Height = 30,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(50, 0, 0, 0)
            };
            button.Click += (sender, e) => OpenTaskMakerWindow(taskListId);
            return button;
        }


        private void DeleteTaskList(int taskListId)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM TaskLists WHERE id = @id;";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskListId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        TaskListPanel.Children.Clear();
                        LoadTaskLists();
                    }
                }
            }
        }

        private ListView CreateTaskListView(TaskList taskList, bool isFirst = false)
        {
            var listView = new ListView
            {
                Margin = new Thickness(0, 0, 0, 10),
                Width = 1000,
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = (Style)FindResource("CustomListViewStyle"),
                View = CreateGridView(isFirst)
            };

            ScrollViewer.SetHorizontalScrollBarVisibility(listView, ScrollBarVisibility.Disabled);

            listView.ItemsSource = GetTasksForList(taskList.Id);

            return listView;
        }

        private GridView CreateGridView(bool isFirst)
        {
            var gridView = new GridView
            {
                ColumnHeaderContainerStyle = (Style)FindResource("CustomGridViewColumnHeaderStyle")
            };

            gridView.Columns.Add(new GridViewColumn
            {
                Width = 285,
                CellTemplate = CreateTitleCellTemplate("Title")
            });

            var columnDefinitions = new (string Header, double Width, DataTemplate CellTemplate)[]
            {
            ("Заметки", 255, CreateNotesCellTemplate("Notes")),
            ("Статус", 135, CreateStatusCellTemplate("Status")),
            ("Дата", 150, CreateDataCellTemplate("FormattedDate")),
            ("Теги", 170, CreateTagsCellTemplate("Tags"))
            };

            foreach (var (Header, Width, CellTemplate) in columnDefinitions)
            {
                gridView.Columns.Add(new GridViewColumn
                {
                    Header = isFirst ? Header : null,
                    Width = Width,
                    CellTemplate = CellTemplate
                });
            }

            return gridView;
        }


        private FrameworkElementFactory CreateTextBoxFactory(string bindingPath, string name, double width, int textLength)
        {
            var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
            textBoxFactory.SetBinding(TextBox.TextProperty, new Binding(bindingPath));

            textBoxFactory.AddHandler(TextBox.MouseDoubleClickEvent, new MouseButtonEventHandler(TextBox_MouseDoubleClick));
            textBoxFactory.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));
            textBoxFactory.AddHandler(TextBox.LostFocusEvent, new RoutedEventHandler(TextBox_LostFocus));

            textBoxFactory.SetValue(TextBox.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#abc3e2")));
            textBoxFactory.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left);
            textBoxFactory.SetValue(TextBox.TextAlignmentProperty, TextAlignment.Left);
            textBoxFactory.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            textBoxFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            textBoxFactory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
            textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(0));
            textBoxFactory.SetValue(TextBox.MarginProperty, new Thickness(0));
            textBoxFactory.SetValue(TextBox.MaxLengthProperty, textLength);
            textBoxFactory.SetValue(TextBox.IsReadOnlyProperty, true);
            textBoxFactory.SetValue(TextBox.FontSizeProperty, 16.0);
            textBoxFactory.SetValue(TextBox.WidthProperty, width);
            textBoxFactory.SetValue(TextBox.NameProperty, name);


            return textBoxFactory;
        }

        private FrameworkElementFactory CreateCheckBoxFactory()
        {
            var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));

            checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsChecked")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            checkBoxFactory.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(CheckBox_Checked));
            checkBoxFactory.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(CheckBox_Unchecked));
            checkBoxFactory.SetValue(CheckBox.StyleProperty, Application.Current.FindResource("CircularCheckBox"));
            checkBoxFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0));

            return checkBoxFactory;
        }


        private DataTemplate CreateTitleCellTemplate(string bindingPath)
        {
            var cellTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var checkBoxFactory = CreateCheckBoxFactory();
            stackPanelFactory.AppendChild(checkBoxFactory);

            var textBoxFactory = CreateTextBoxFactory(bindingPath, "Title", 265.0, 50);
            stackPanelFactory.AppendChild(textBoxFactory);

            cellTemplate.VisualTree = stackPanelFactory;

            return cellTemplate;
        }

        private DataTemplate CreateNotesCellTemplate(string bindingPath)
        {
            var cellTemplate = new DataTemplate();

            var textBoxFactory = CreateTextBoxFactory(bindingPath, "Notes", 245.0, 1000);
            textBoxFactory.SetValue(TextBox.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            textBoxFactory.SetValue(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            textBoxFactory.SetValue(TextBox.AcceptsReturnProperty, false);
            textBoxFactory.SetValue(TextBox.MaxHeightProperty, 100.0);

            var scrollViewerFactory = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewerFactory.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            scrollViewerFactory.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scrollViewerFactory.SetValue(ScrollViewer.CanContentScrollProperty, true);
            scrollViewerFactory.AppendChild(textBoxFactory);

            cellTemplate.VisualTree = scrollViewerFactory;

            return cellTemplate;
        }


        private DataTemplate CreateStatusCellTemplate(string bindingPath)
        {
            var cellTemplate = new DataTemplate();
            var comboBoxFactory = new FrameworkElementFactory(typeof(ComboBox));

            comboBoxFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding(bindingPath)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            comboBoxFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(ComboBox_SelectionChanged));
            comboBoxFactory.SetValue(ComboBox.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34344a")));
            comboBoxFactory.SetValue(ComboBox.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BED5F2")));
            comboBoxFactory.SetValue(ComboBox.ItemsSourceProperty, new string[] { "Планируется", "Выполняется", "Завершено" });
            comboBoxFactory.SetValue(ComboBox.StyleProperty, Application.Current.FindResource("FullClickableComboBox"));
            comboBoxFactory.SetValue(ComboBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            comboBoxFactory.SetValue(ComboBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            comboBoxFactory.SetValue(ComboBox.FontSizeProperty, 16.0);
            comboBoxFactory.SetValue(ComboBox.WidthProperty, 125.0);

            cellTemplate.VisualTree = comboBoxFactory;

            return cellTemplate;
        }

        private DataTemplate CreateDataCellTemplate(string bindingPath)
        {
            var cellTemplate = new DataTemplate();
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));

            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding(bindingPath));
            textBlockFactory.AddHandler(TextBlock.MouseLeftButtonDownEvent, new MouseButtonEventHandler(TextBlock_MouseLeftButtonDown));
            textBlockFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#abc3e2")));
            textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, 16.0);
            textBlockFactory.SetValue(TextBlock.WidthProperty, 150.0);

            cellTemplate.VisualTree = textBlockFactory;

            return cellTemplate;
        }


        private DataTemplate CreateTagsCellTemplate(string bindingPath)
        {
            var cellTemplate = new DataTemplate();

            var itemsControlFactory = new FrameworkElementFactory(typeof(ItemsControl));
            itemsControlFactory.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(bindingPath));
            itemsControlFactory.SetValue(ItemsControl.ItemTemplateProperty, CreateTagItemTemplate());
            itemsControlFactory.AddHandler(ItemsControl.MouseLeftButtonDownEvent, new MouseButtonEventHandler(ItemsControl_MouseLeftButtonDown));

            cellTemplate.VisualTree = itemsControlFactory;

            return cellTemplate;
        }

        private DataTemplate CreateTagItemTemplate()
        {
            var itemTemplate = new DataTemplate();
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(3));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 0, 3, 0));
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding("Color")
            {
                Converter = new InputValidator.StringToBrushConverter()
            });

            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Title"));
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            textBlockFactory.SetValue(TextBlock.WidthProperty, 170.0);
            textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(textBlockFactory);

            itemTemplate.VisualTree = borderFactory;

            return itemTemplate;
        }


        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
        }


        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.IsReadOnly)
            {
                textBox.IsReadOnly = false;
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OpenTaskEditorWindow((sender as TextBlock)?.DataContext as TaskItem);
            }
        }

        private void ItemsControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OpenTaskEditorWindow((sender as ItemsControl)?.DataContext as TaskItem);
            }
        }


        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(caretIndex, Environment.NewLine);
                textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                textBox.IsReadOnly = true;
                if (textBox.DataContext is TaskItem taskItem)
                {
                    string newValue = textBox.Text;
                    string columnName = GetColumnNameFromTextBoxName(textBox.Name);
                    UpdateTaskFieldInDatabase(taskItem, columnName, newValue);
                }
                e.Handled = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var taskItem = textBox.DataContext as TaskItem;
                if (taskItem != null)
                {
                    string newValue = textBox.Text;
                    string columnName = GetColumnNameFromTextBoxName(textBox.Name);
                    UpdateTaskFieldInDatabase(taskItem, columnName, newValue);
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var taskItem = (comboBox.DataContext as TaskItem);
            if (comboBox != null && taskItem != null)
            {
                string newStatus = comboBox.SelectedValue as string;
                UpdateTaskFieldInDatabase(taskItem, "status", newStatus);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var taskItem = checkBox.DataContext as TaskItem;
            if (taskItem != null && checkBox.IsChecked != taskItem.OriginalIsChecked)
            {
                UpdateTaskIsCheckedInDatabase(taskItem.Id, checkBox.IsChecked ?? true);
                taskItem.OriginalIsChecked = checkBox.IsChecked ?? true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var taskItem = checkBox.DataContext as TaskItem;
            if (taskItem != null && checkBox.IsChecked != taskItem.OriginalIsChecked)
            {
                UpdateTaskIsCheckedInDatabase(taskItem.Id, checkBox.IsChecked ?? false);
                taskItem.OriginalIsChecked = checkBox.IsChecked ?? false;
            }
        }

        private void OpenTaskMakerWindow(int taskListId)
        {
            var taskMakerWindow = new TaskEditorWindow(currentProject, listID: taskListId);
            var result = taskMakerWindow.ShowDialog();
            if (result == true)
            {
                TaskListPanel.Children.Clear();
                LoadTaskLists();
            }
        }
        private void OpenTaskEditorWindow(TaskItem taskItem)
        {
            if (taskItem != null)
            {
                new TaskEditorWindow(currentProject, taskItem).ShowDialog();
                TaskListPanel.Children.Clear();
                LoadTaskLists();
            }
        }

        private void UpdateTaskIsCheckedInDatabase(int taskId, bool isChecked)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "UPDATE Tasks SET isChecked = @isChecked WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@isChecked", isChecked);
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        private async Task UpdateTaskFieldInDatabase(TaskItem taskItem, string columnName, object newValue)
        {
            object originalValue = columnName switch
            {
                "title" => taskItem.OriginalTitle,
                "description" => taskItem.OriginalNotes,
                _ => null
            };

            if (!object.Equals(newValue, originalValue))
            {
                using (var conn = App.GetConnection())
                {
                    await conn.OpenAsync();
                    string query = $"UPDATE Tasks SET {columnName} = @newValue WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@newValue", newValue);
                        cmd.Parameters.AddWithValue("@id", taskItem.Id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                switch (columnName)
                {
                    case "title":
                        taskItem.OriginalTitle = (string)newValue;
                        break;
                    case "description":
                        taskItem.OriginalNotes = (string)newValue;
                        break;
                }

                await App.NotificationManagerInstance.RefreshNotifications();
            }
        }

        private string GetColumnNameFromTextBoxName(string textBoxName)
        {
            switch (textBoxName)
            {
                case "Title":
                    return "title";
                case "Notes":
                    return "description";
                default:
                    return null;
            }
        }

        private List<TaskList> GetTaskListsForProject(int projectID)
        {
            var taskLists = new List<TaskList>();
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "SELECT id, title FROM TaskLists WHERE projectID = @projectID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectID", projectID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            taskLists.Add(new TaskList
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title")
                            });
                        }
                    }
                }
            }
            return taskLists;
        }

        private int GetTaskCountForList(int taskListId)
        {
            int taskCount = 0;
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Tasks WHERE taskListID = @taskListId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskListId", taskListId);
                    taskCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return taskCount;
        }

        public static List<Tag> GetTagsForTask(int taskId)
        {
            var tags = new List<Tag>();
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT Tags.id, Tags.title, Tags.color
                    FROM Tags INNER JOIN TagsSet ON Tags.id = TagsSet.tagID
                    WHERE TagsSet.taskID = @taskId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tags.Add(new Tag
                            {
                                ID = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Color = reader.GetString("color")
                            });
                        }
                    }
                }
            }
            return tags;
        }


        private List<TaskItem> GetTasksForList(int taskListId)
        {
            var tasks = new List<TaskItem>();
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT id, title, description, status, dateTime, isChecked, isRepeat, hasNotification,taskListID, projectID
                    FROM Tasks
                    WHERE taskListID = @taskListId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskListId", taskListId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var task = new TaskItem
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Notes = reader.GetString("description"),
                                Status = reader.GetString("status"),
                                Date = reader.IsDBNull(reader.GetOrdinal("dateTime")) ? null : (DateTime?)reader.GetDateTime("dateTime"),
                                IsChecked = !reader.IsDBNull(reader.GetOrdinal("isChecked")) && reader.GetBoolean("isChecked"),
                                Tags = GetTagsForTask(reader.GetInt32("id")),
                                IsRepeat = reader.GetBoolean("isRepeat"),
                                HasNotification = reader.GetBoolean("hasNotification"),
                                TaskListID = reader.GetInt32("taskListID"),
                                ProjectID = reader.GetInt32("projectID"),
                                OriginalTitle = reader.GetString("title"),
                                OriginalNotes = reader.GetString("description"),
                                OriginalIsChecked = !reader.IsDBNull(reader.GetOrdinal("isChecked")) && reader.GetBoolean("isChecked")
                            };
                            tasks.Add(task);
                        }
                    }
                }
            }
            return tasks;
        }

       
    }
}
