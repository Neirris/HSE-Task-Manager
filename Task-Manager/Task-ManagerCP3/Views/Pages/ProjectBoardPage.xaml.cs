using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Task_ManagerCP3.Models;
using Task_ManagerCP3.Views.Pages;

namespace Task_ManagerCP3
{
    public partial class ProjectBoardPage : Page
    {
        private Project currentProject;
        private ProjectManager projectManager;

        private string? originalProjectName;
        public ProjectBoardPage(Project project, ProjectManager manager)
        {
            InitializeComponent();
            currentProject = project;
            projectManager = manager;
            SetProjectDetails();
            LoadTaskManager();
        }
        private void SetProjectDetails()
        {
            logo_projectIcon.Background = new BrushConverter().ConvertFromString(currentProject.Color) as SolidColorBrush;
            logo_projectIconText.Content = ProjectManager.GetInitials(currentProject.Title);
            logo_projectName.Text = currentProject.Title;
        }

        private void LoadTaskManager()
        {
            select_CalendarUnderline.Visibility = Visibility.Hidden;
            if (FindName("TasksFrame") is Frame tasksFrame)
            {
                tasksFrame.Navigate(new TaskManagerPage(currentProject));
            }
        }

        private void select_Tasks_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            select_TasksUnderline.Visibility = Visibility.Visible;
            select_CalendarUnderline.Visibility = Visibility.Hidden;
            btnAddTaskList.Visibility = Visibility.Visible;
            if (FindName("TasksFrame") is Frame tasksFrame)
            {
                tasksFrame.Navigate(new TaskManagerPage(currentProject));
            }
        }

        private void select_Calendar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            select_TasksUnderline.Visibility = Visibility.Hidden;
            select_CalendarUnderline.Visibility = Visibility.Visible;
            btnAddTaskList.Visibility = Visibility.Hidden;
            if (FindName("TasksFrame") is Frame tasksFrame)
            {
                tasksFrame.Navigate(new CalendarManagerPage(currentProject));
            }
        }

        private void logo_projectName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            originalProjectName = logo_projectName.Text;
            logo_projectName.IsReadOnly = false;
            logo_projectName.Focusable = true;
            logo_projectName.Background = new BrushConverter().ConvertFromString("#292b2f") as SolidColorBrush;
        }

        private void logo_projectName_LostFocus(object sender, RoutedEventArgs? e)
        {
            logo_projectName.IsReadOnly = true;
            logo_projectName.Focusable = false;
            logo_projectName.Background = Brushes.Transparent;
            if (logo_projectName.Text != originalProjectName && !logo_projectName.IsReadOnly)
            {
                logo_projectName.Text = originalProjectName;
            }
        }

        private void GoToEmptyPage()
        {
            if (NavigationService != null)
            {
                var emptyPage = new Page();
                NavigationService.Navigate(emptyPage);
            }
        }

        private void logo_projectName_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                if (string.IsNullOrEmpty(logo_projectName.Text))
                {
                    MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить проект?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        projectManager.EditProjectName(currentProject, "");
                        GoToEmptyPage();
                    }
                    else
                    {
                        logo_projectName.Text = originalProjectName;
                        logo_projectName_LostFocus(logo_projectName, null);
                    }
                }
                else
                {
                    if (logo_projectName.Text != originalProjectName)
                    {
                        projectManager.EditProjectName(currentProject, logo_projectName.Text);
                    }
                    logo_projectName_LostFocus(logo_projectName, null);
                }
            }
        }

        private void logo_projectIconText_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ColorPickerWindow colorPickerDialog = new ColorPickerWindow();
            if (colorPickerDialog.ShowDialog() == true)
            {
                Color selectedColor = colorPickerDialog.SelectedColor;
                logo_projectIcon.Background = new SolidColorBrush(selectedColor);
                projectManager.EditProjectColor(currentProject, selectedColor.ToString());
            }

        }

        private void AddTaskList_Click(object sender, RoutedEventArgs e)
        {
            var listMakerWindow = new ListMakerWindow(currentProject);
            var result = listMakerWindow.ShowDialog(); 
            if (result == true) 
            {
                if (FindName("TasksFrame") is Frame tasksFrame)
                {
                    tasksFrame.Navigate(new TaskManagerPage(currentProject));
                }
            }
        }

    }
}
