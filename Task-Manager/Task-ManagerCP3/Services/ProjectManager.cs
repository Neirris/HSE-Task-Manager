using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using Task_ManagerCP3.Models;

namespace Task_ManagerCP3
{
    public class ProjectManager
    {
        private StackPanel projectPanel;
        private Window parentWindow;
        private const int MaxProjects = 15;

        public ProjectManager(StackPanel _projectPanel, Window _parentWindow)
        {
            projectPanel = _projectPanel;
            parentWindow = _parentWindow;
            LoadProjects();
        }

        public void LoadProjects()
        {
            var projects = GetUserProjects();
            DisplayProjects(projects);
        }

        private List<Project> GetUserProjects()
        {
            var projects = new List<Project>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                int userID = App.GetUserId(App.AuthToken, conn);

                const string query = "SELECT id, title, color FROM projects WHERE userID = @userID";
                using (var projCmd = new MySqlCommand(query, conn))
                {
                    projCmd.Parameters.AddWithValue("@userID", userID);

                    using (var reader = projCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var project = new Project
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Color = reader.IsDBNull(reader.GetOrdinal("color")) ? "#FFFFFF" : reader.GetString("color"),
                                UserId = userID

                            };
                            projects.Add(project);
                        }
                    }
                }
            }
            return projects;
        }

        private void DisplayProjects(List<Project> projects)
        {
            projectPanel.Children.Clear();

            foreach (var project in projects)
            {
                projectPanel.Children.Add(CreateProjectUIElement(project));
            }

            if (projects.Count < MaxProjects)
            {
                projectPanel.Children.Add(CreateAddProjectButton());
            }
        }

        private UIElement CreateProjectUIElement(Project project)
        {
            var projectItemPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var projectIconBorder = new Border
            {
                Background = new BrushConverter().ConvertFromString(project.Color) as SolidColorBrush,
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(5, 0, 5, 5)
            };

            var projectIcon = new Label
            {
                Content = GetInitials(project.Title),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            projectIconBorder.Child = projectIcon;

            var projectName = new TextBlock
            {
                Text = project.Title,
                Foreground = new BrushConverter().ConvertFromString("#abc3e2") as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Width = 100,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            projectItemPanel.Children.Add(projectIconBorder);
            projectItemPanel.Children.Add(projectName);

            projectItemPanel.MouseLeftButtonDown += (sender, e) => ProjectItemPanel_MouseLeftButtonDown(project);

            return projectItemPanel;
        }

        private void ProjectItemPanel_MouseLeftButtonDown(Project project)
        {
            if (parentWindow.FindName("MainFrame") is Frame mainFrame)
            {
                mainFrame.Navigate(new ProjectBoardPage(project, this));
            }
        }


        private Button CreateAddProjectButton()
        {
            var addProjectButton = new Button
            {
                Name = "btnAddProject",
                Content = "+",
                FontSize = 18,
                Width = 25,
                Height = 25,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            addProjectButton.Click += AddProject_Click;
            return addProjectButton;
        }

        public static string GetRandomColorHex()
        {
            Random random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }


        public static string GetInitials(string name)
        {
            return new string(name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Take(2)
                                  .Select(s => s[0])
                                  .ToArray())
                                  .ToUpper();
        }

        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            var projectMakerWindow = new ProjectMakerWindow();
            projectMakerWindow.ProjectCreated += (projectName) =>
            {
                CreateNewProject(projectName);
            };
            projectMakerWindow.ShowDialog();
        }


        public static void CreateNewList(Project project, string listName)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();

                using (var projCmd = new MySqlCommand("INSERT INTO TaskLists (projectID, title, userID) VALUES (@projectID, @title, @userID)", conn))
                {
                    projCmd.Parameters.AddWithValue("@projectID", project.Id);
                    projCmd.Parameters.AddWithValue("@title", listName);
                    projCmd.Parameters.AddWithValue("@userID", project.UserId);
                    projCmd.ExecuteNonQuery();
                }
            }

        }

        private void CreateNewProject(string title)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                int userID = App.GetUserId(App.AuthToken, conn);
                string color = GetRandomColorHex();

                using (var projCmd = new MySqlCommand("INSERT INTO projects (title, color, userID) VALUES (@title, @color, @userID)", conn))
                {
                    projCmd.Parameters.AddWithValue("@title", title);
                    projCmd.Parameters.AddWithValue("@color", color);
                    projCmd.Parameters.AddWithValue("@userID", userID);
                    projCmd.ExecuteNonQuery();
                }
            }

            LoadProjects();
        }

        public void EditProjectName(Project project, string new_projectName)
        {
            using (MySqlConnection conn = App.GetConnection())
            {
                conn.Open();

                if (new_projectName == "")
                {
                    string queryDelete = "DELETE FROM Projects WHERE id = @projectID";
                    using (MySqlCommand cmd = new MySqlCommand(queryDelete, conn))
                    {
                        cmd.Parameters.AddWithValue("@projectID", project.Id);
                        int result = cmd.ExecuteNonQuery();
                        MessageBox.Show("Проект успешно удалён!");
                    }
                }
                else
                {
                    string queryUpdate = "UPDATE Projects SET title = @newTitle WHERE id = @projectID";
                    using (MySqlCommand cmd = new MySqlCommand(queryUpdate, conn))
                    {
                        cmd.Parameters.AddWithValue("@newTitle", new_projectName);
                        cmd.Parameters.AddWithValue("@projectID", project.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadProjects();
            }
        }

        public void EditProjectColor(Project project, string new_projectColor)
        {
            using (MySqlConnection conn = App.GetConnection())
            {
                conn.Open();
                string queryUpdate = "UPDATE Projects SET color = @newColor WHERE id = @projectID";
                using (MySqlCommand cmd = new MySqlCommand(queryUpdate, conn))
                {
                    cmd.Parameters.AddWithValue("@newColor", new_projectColor);
                    cmd.Parameters.AddWithValue("@projectID", project.Id);
                    cmd.ExecuteNonQuery();
                }
                LoadProjects();
            }
        }
    }
}



