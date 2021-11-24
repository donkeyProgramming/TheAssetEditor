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
using System.Windows.Shapes;

namespace CommonControls.Simple
{
    /// <summary>
    /// Interaction logic for ControllerHostWindow.xaml
    /// </summary>
    public partial class ControllerHostWindow : Window
    {
        public ControllerHostWindow()
        {
            InitializeComponent();
            
        }

        public ControllerHostWindow(bool alwaysTopMost)
        {
            if(alwaysTopMost)
                Deactivated += Window_Deactivated;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;

        }
    }
}
