using System.Windows;
using System.Windows.Input;

namespace Task_ManagerCP3
{
    public partial class ProjectMakerWindow : Window
    {
        public event Action<string>? ProjectCreated;

        public ProjectMakerWindow()
        {
            InitializeComponent();
        }

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputProjectName.Text))
            {
                MessageBox.Show("Название не введено!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                ProjectCreated?.Invoke(inputProjectName.Text);
                Close();
            }
        }

        private void CancelProject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

    }
}
