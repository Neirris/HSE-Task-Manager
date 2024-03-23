using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Task_ManagerCP3.Models;

namespace Task_ManagerCP3.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            DashboardPage_Loaded();
        }

        private void DashboardPage_Loaded()
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                var userId = App.GetUserId(App.AuthToken, conn);

                var closestTasks = GetClosestTasksWithTags(userId);
                UpdateTasks(closestTasks);
                UpdateProjectStatus(userId);

            }
        }

        private void UpdateProjectStatus(int userId)
        {
            var totalProjects = GetTotalProjectsCount(userId);
            var completedProjects = GetCompletedProjectsCount(userId);
            var runningProjects = GetRunningProjectsCount(userId);

            allProjectValue.Text = totalProjects.ToString();
            completedProjectValue.Text = completedProjects.ToString();
            runProjectValue.Text = runningProjects.ToString();

            UpdateProgressBar(completedProjects, totalProjects);

            var lastModifiedProjects = GetLastModifiedProject(userId);
            UpdateLastModifiedProjects(lastModifiedProjects);

        }

        private void UpdateLastModifiedProjects(List<Project> projects)
        {
            if (projects.Count > 0)
            {
                Project project1 = projects[0];
                projectIcon1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(project1.Color));
                projectIconText1.Content = ProjectManager.GetInitials(project1.Title);
                ProjectBlock1Text.Text = project1.Title;
                ProjectBlock1Task1Text.Text = GetLastTasksForProject(project1.Id);
                ProjectBlock1.Visibility = Visibility.Visible;
            }
            else
            {
                ProjectBlock1.Visibility = Visibility.Collapsed;
            }

            if (projects.Count > 1)
            {
                Project project2 = projects[1];
                projectIcon2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(project2.Color));
                projectIconText2.Content = ProjectManager.GetInitials(project2.Title);
                ProjectBlock2Text.Text = project2.Title;
                ProjectBlock1Task2Text.Text = GetLastTasksForProject(project2.Id);
                ProjectBlock2.Visibility = Visibility.Visible;
            }
            else
            {
                ProjectBlock2.Visibility = Visibility.Collapsed;
            }
        }


        private void UpdateProgressBar(int completedProjects, int totalProjects)
        {
            if (totalProjects > 0)
            {
                double completedPercentage = (double)completedProjects / totalProjects * 100;
                completedProjectProgressBar.Value = completedPercentage;
                completedProjectProgressBarText.Text = $"{completedPercentage:0.##}%";
            }
            else
            {
                completedProjectProgressBar.Value = 0;
                completedProjectProgressBarText.Text = "0%";
            }
        }


        private void UpdateTasks(List<TaskItem> tasks)
        {
            var brushConverter = new BrushConverter();

            TaskBlock1.Visibility = Visibility.Collapsed;
            TaskBlock2.Visibility = Visibility.Collapsed;
            TaskBlock3.Visibility = Visibility.Collapsed;


            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var tag = task.Tags.FirstOrDefault();
                var tagColor = tag != null ? tag.Color : "#787891";
                Brush backgroundColor = (Brush)brushConverter.ConvertFromString(tagColor);


                switch (i)
                {
                    case 0:
                        TaskTitle1.Text = task.Title;
                        TaskTag1.Text = tag?.Title ?? "";
                        TaskDate1.Text = task.FormattedDate;
                        TaskBlock1.Background = backgroundColor;
                        TaskBlock1.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        TaskTitle2.Text = task.Title;
                        TaskTag2.Text = tag?.Title ?? "";
                        TaskDate2.Text = task.FormattedDate;
                        TaskBlock2.Background = backgroundColor;
                        TaskBlock2.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        TaskTitle3.Text = task.Title;
                        TaskTag3.Text = tag?.Title ?? "";
                        TaskDate3.Text = task.FormattedDate;
                        TaskBlock3.Background = backgroundColor;
                        TaskBlock3.Visibility = Visibility.Visible;
                        break;
                }
            }

            if (tasks.Count == 0)
            {
                TaskTitle1.Text = "Запланированных задач нет";
                TaskTitle1.HorizontalAlignment = HorizontalAlignment.Center;
                TaskTitle1.VerticalAlignment = VerticalAlignment.Center;
                TaskTitle1.Margin = new Thickness(0,60,0,0);
                TaskTitle1.Width = 710;
                TaskTag1.Visibility = Visibility.Collapsed;
                TaskDate1.Visibility = Visibility.Collapsed;
                TaskBlock1.Visibility = Visibility.Visible;
                TaskBlock1.Width = 710;
            }
        }

        public static int GetTotalProjectsCount(int userId)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(DISTINCT projectID) FROM Tasks WHERE userID = @userId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static int GetCompletedProjectsCount(int userId)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT COUNT(DISTINCT projectID)
                    FROM Tasks
                    WHERE userID = @userId AND projectID NOT IN (
                        SELECT DISTINCT projectID
                        FROM Tasks
                        WHERE userID = @userId AND status != 'Завершено'
                )";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static int GetRunningProjectsCount(int userId)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT COUNT(DISTINCT projectID)
                    FROM Tasks
                    WHERE userID = @userId AND projectID IN (
                        SELECT DISTINCT projectID
                        FROM Tasks
                        WHERE userID = @userId AND status != 'Завершено'
                    )";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static List<Project> GetLastModifiedProject(int userId)
        {
            List<Project> projects = new List<Project>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT p.id, p.title, p.color
            FROM Projects p
            JOIN TaskAudit ta ON p.id = ta.projectID
            WHERE p.userID = @userId
            GROUP BY p.id
            ORDER BY MAX(ta.changeTime) DESC
            LIMIT 2;";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            projects.Add(new Project
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Color = reader.IsDBNull(reader.GetOrdinal("color")) ? string.Empty : reader.GetString("color"),
                                UserId = userId
                            });
                        }
                    }
                }
            }

            return projects;
        }



        public static string GetLastTasksForProject(int projectId)
        {
            string lastTaskTitle = string.Empty;

            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT title
            FROM TaskAudit
            WHERE projectID = @projectId
            ORDER BY changeTime DESC
            LIMIT 1;";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        lastTaskTitle = result.ToString();
                    }
                }
            }

            return lastTaskTitle;
        }


        public static List<TaskItem> GetClosestTasksWithTags(int userId)
        {
            var closestTasks = new List<TaskItem>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT id, title, dateTime, status, taskListID, projectID
                    FROM Tasks
                    WHERE userID = @userId AND dateTime >= CURRENT_TIMESTAMP
                    ORDER BY dateTime ASC
                    LIMIT 3;";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var task = new TaskItem
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Date = reader.IsDBNull(reader.GetOrdinal("dateTime")) ? (DateTime?)null : reader.GetDateTime("dateTime"),
                                Status = reader.GetString("status"),
                                Tags = TaskManagerPage.GetTagsForTask(reader.GetInt32("id")),
                                TaskListID = reader.GetInt32("taskListID"),
                                ProjectID = reader.GetInt32("projectID")
                            };
                            closestTasks.Add(task);
                        }
                    }
                }
            }

            return closestTasks;
        }
    }
}
