using System;
using System.Windows;

namespace Shared.Ui.BaseDialogs
{
    public partial class ControllerHostWindow : Window
    {
        public ControllerHostWindow()
        {
            InitializeComponent();
        }

        public ControllerHostWindow(bool alwaysTopMost = false, ResizeMode resizeMode = ResizeMode.NoResize)
        {
            if (alwaysTopMost)
                Deactivated += Window_Deactivated;

            ResizeMode = resizeMode;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }
    }
}
