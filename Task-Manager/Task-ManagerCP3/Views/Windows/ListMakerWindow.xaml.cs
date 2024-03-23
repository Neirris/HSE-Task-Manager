using System.Windows;
using System.Windows.Input;
using Task_ManagerCP3.Models;

namespace Task_ManagerCP3
{
    public partial class ListMakerWindow : Window
    {
        private Project currentProject;
        public ListMakerWindow(Project project)
        {
            InitializeComponent();
            currentProject = project;
        }

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CancelList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputListName.Text))
            {
                MessageBox.Show("Название не введено!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                ProjectManager.CreateNewList(currentProject, inputListName.Text);
                DialogResult = true;
                Close();
            }
        }

    }
}
