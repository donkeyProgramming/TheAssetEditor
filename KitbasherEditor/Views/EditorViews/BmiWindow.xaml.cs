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

namespace KitbasherEditor.Views.EditorViews
{
    /// <summary>
    /// Interaction logic for BmiWindow.xaml
    /// </summary>
    public partial class BmiWindow : Window
    {
        public BmiWindow()
        {
            InitializeComponent();
            this.Deactivated += Window_Deactivated;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }
    }
}
