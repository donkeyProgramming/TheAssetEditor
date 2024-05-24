using Shared.Ui.Common.MenuSystem;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KitbasherEditor.Views
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.KeyUp += HandleKeyPress;
                window.KeyDown += HandleKeyDown;
            }
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                e.Handled = true;
                return;
            }

            if (DataContext is IKeyboardHandler keyboardHandler)
            {
                var res = keyboardHandler.OnKeyReleased(e.Key, e.SystemKey, Keyboard.Modifiers);
                if (res)
                    e.Handled = true;
            }
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is IKeyboardHandler keyboardHandler)
            {
                keyboardHandler.OnKeyDown(e.Key, e.SystemKey, Keyboard.Modifiers);
            }
        }
    }
}
