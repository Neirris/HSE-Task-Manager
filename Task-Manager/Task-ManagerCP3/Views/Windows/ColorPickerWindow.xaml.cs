using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Task_ManagerCP3
{
    public partial class ColorPickerWindow : Window
    {
        public Color SelectedColor { get; private set; }

        public ColorPickerWindow()
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

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (colorPicker.SelectedColor.HasValue)
            {
                SelectedColor = colorPicker.SelectedColor.Value;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ColorPickerCancel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

    }
}
