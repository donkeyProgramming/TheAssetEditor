using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.MenuBarViews;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
           window.KeyUp += HandleKeyPress;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if (DataContext is IKeyboardHandler keyboardHandler)
            {
                var res = keyboardHandler.HandleKeyUp(e.Key, Keyboard.Modifiers);
                if (res)
                    e.Handled = true;
            }
        }
    }
}
