using MindFusion.Scheduling;
using MindFusion.Scheduling.Wpf;
using MindFusion.Drawing;
using MySql.Data.MySqlClient;
using System.Windows.Controls;
using Task_ManagerCP3.Models;
using System.Windows.Media;
using Task_ManagerCP3.Services;


namespace Task_ManagerCP3.Views.Pages
{
    public partial class CalendarManagerPage : Page
    {
        Project currentProject;
        public CalendarManagerPage(Project project)
        {
            InitializeComponent();
            currentProject = project;
            LoadTasksIntoCalendar();
        }

        private void LoadTasksIntoCalendar()
        {
            schedule.AllowInplaceEdit = false;
            schedule.ItemSettings = new ItemSettings();
            schedule.ItemClick += Schedule_ItemClick;

            var tasks = GetTasksForProject(currentProject.Id);
            foreach (var task in tasks)
            {
                if (task.Date.HasValue)
                {
                    var startTime = task.Date.Value;
                    var endTime = task.Date.Value.AddHours(1);
                    var item = new Appointment()
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        HeaderText = task.Title.Length > 15 ? task.Title.Substring(0, 15) + "..." : task.Title,
                        Tag = task
                    };

                    if (task.Tags.Any())
                    {
                        var tag = task.Tags.First();
                        var tagColor = (Color)ColorConverter.ConvertFromString(tag.Color);

                        var itemStyle = new CalendarStyle();
                        itemStyle.Background = new SolidColorBrush(tagColor); 
                        itemStyle.Foreground = Brushes.White;

                        schedule.SetItemStyle(item, itemStyle);
                    }
                    else
                    {
                        var defaultItemStyle = new CalendarStyle();
                        defaultItemStyle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#232332")); 
                        defaultItemStyle.Foreground = Brushes.White; 

                        schedule.SetItemStyle(item, defaultItemStyle);
                    }

                    schedule.Schedule.Items.Add(item);

                    var repeatTasks = GetRepeatTasksForTask(task.Id);
                    foreach (var repeatTask in repeatTasks)
                    {

                        if (repeatTask.RepeatType == "Еженедельно")
                        {
                            DateTime nextWeekDate = task.Date.Value.AddDays(7 - (int)task.Date.Value.DayOfWeek + 1);

                            for (DateTime dt = nextWeekDate; dt < DateTime.Now.AddYears(1); dt = dt.AddDays(7))
                            {
                                var repeatedItem = new Appointment()
                                {
                                    StartTime = dt,
                                    EndTime = dt.AddHours(1),
                                    HeaderText = task.Title,
                                    Tag = task 
                                };
                                schedule.Schedule.Items.Add(repeatedItem);
                            }
                        }
                        else if (repeatTask.RepeatType == "Дата" && repeatTask.RepeatDateTime.HasValue)
                        {
                            var repeatDateTime = repeatTask.RepeatDateTime.Value;
                            var repeatedItem = new Appointment()
                            {
                                StartTime = repeatDateTime,
                                EndTime = repeatDateTime.AddHours(1),
                                HeaderText = task.Title,
                                Tag = task 
                            };
                            schedule.Schedule.Items.Add(repeatedItem);
                        }
                    }
                }
            }
        }

        private void Schedule_ItemClick(object sender, ItemMouseEventArgs e)
        {
            if (e.Item is Appointment appointment)
            {
                if (appointment.Tag is TaskItem taskItem)
                {
                    var editorWindow = new TaskEditorWindow(currentProject, taskItem);
                    var dialogResult = editorWindow.ShowDialog();

                    if (dialogResult.HasValue && dialogResult.Value)
                    {
                        schedule.Schedule.Items.Clear(); 
                        LoadTasksIntoCalendar();
                    }
                }
            }
        }

        private List<RepeatInfo> GetRepeatTasksForTask(int taskId)
        {
            var repeatTasks = new List<RepeatInfo>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT id, taskID, repeatType, repeatDateTime
            FROM RepeatTasks
            WHERE taskID = @taskId;";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var repeatTask = new RepeatInfo
                            {
                                ID = reader.GetInt32("id"),
                                TaskID = reader.GetInt32("taskID"),
                                RepeatType = reader.IsDBNull(reader.GetOrdinal("repeatType")) ? null : reader.GetString("repeatType"),
                                RepeatDateTime = reader.IsDBNull(reader.GetOrdinal("repeatDateTime")) ? (DateTime?)null : reader.GetDateTime("repeatDateTime")
                            };

                            repeatTasks.Add(repeatTask);
                        }
                    }
                }
            }

            return repeatTasks;
        }

        private List<TaskItem> GetTasksForProject(int projectId)
        {
            var tasks = new List<TaskItem>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT id, title, description, status, dateTime, isRepeat, hasNotification, isChecked, taskListID, projectID
                    FROM Tasks
                    WHERE projectID = @projectId AND dateTime IS NOT NULL
                    ORDER BY dateTime ASC;";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
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
                                Tags = TaskManagerPage.GetTagsForTask(reader.GetInt32("id")),
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
