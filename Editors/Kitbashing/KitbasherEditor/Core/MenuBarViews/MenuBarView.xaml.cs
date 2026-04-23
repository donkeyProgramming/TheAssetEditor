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
                window.KeyUp += HandleKeyUp;
                window.KeyDown += HandleKeyDown;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.KeyUp -= HandleKeyUp;
                window.KeyDown -= HandleKeyDown;
            }
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox && Keyboard.Modifiers == ModifierKeys.None)
                return;

            if (DataContext is IKeyboardHandler keyboardHandler)
                keyboardHandler.OnKeyReleased(e.Key, e.SystemKey, Keyboard.Modifiers);
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox && Keyboard.Modifiers == ModifierKeys.None)
                return;

            if (DataContext is IKeyboardHandler keyboardHandler)
            {
                keyboardHandler.OnKeyDown(e.Key, e.SystemKey, Keyboard.Modifiers);
            }
        }
    }
}
