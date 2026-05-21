using Editors.KitbasherEditor.Core.MenuBarViews;
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
        private Window? _parentWindow;
        private MenuBarKeyboardDispatcher? _dispatcher;

        public MenuBarView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            _dispatcher = new MenuBarKeyboardDispatcher(
                () => IsVisible,
                () => DataContext as IKeyboardHandler);

            if (_parentWindow != null)
            {
                _parentWindow.KeyUp += HandleKeyUp;
                _parentWindow.KeyDown += HandleKeyDown;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.KeyUp -= HandleKeyUp;
                _parentWindow.KeyDown -= HandleKeyDown;
                _parentWindow = null;
            }

            _dispatcher = null;
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            _dispatcher?.HandleKeyUp(e.Key, e.SystemKey, Keyboard.Modifiers, e.OriginalSource is TextBox);
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            _dispatcher?.HandleKeyDown(e.Key, e.SystemKey, Keyboard.Modifiers, e.OriginalSource is TextBox);
        }
    }
}
