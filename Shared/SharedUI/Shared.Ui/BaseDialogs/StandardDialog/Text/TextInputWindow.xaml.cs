using System.Windows;
using System.Windows.Input;

namespace CommonControls.BaseDialogs
{
    /// <summary>
    /// Interaction logic for TextInputWindow.xaml
    /// </summary>
    public partial class TextInputWindow : Window
    {
        public TextInputWindow()
        {
            InitializeComponent();
        }

        public TextInputWindow(string title, string initialValue = "", bool focusTextInput = false)
        {
            InitializeComponent();
            Title = title;
            TextValue = initialValue;
            Owner = Application.Current.MainWindow;

            if (focusTextInput)
            {
                TextBoxItem.Focus();
            }
        }

        public string TextValue
        {
            get => TextBoxItem.Text;
            set => TextBoxItem.Text = value;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Key_Down(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }

            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
