using System.Windows;
using System.Windows.Controls;

namespace AssetEditor.Views
{
    /// <summary>
    /// Interaction logic for MenuBarView.xaml
    /// </summary>
    public partial class MenuBarView : UserControl
    {
        public MenuBarView()
        {
            InitializeComponent();
        }

        // Same path as the title bar's close button, so the unsaved-changes prompt still runs.
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
                SystemCommands.CloseWindow(window);
        }
    }
}
