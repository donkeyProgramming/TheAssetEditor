using System.Windows;

namespace Shared.Ui.BaseDialogs.ToolSelector
{
    public partial class ToolSelectorWindow : Window
    {
        public ToolSelectorWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
