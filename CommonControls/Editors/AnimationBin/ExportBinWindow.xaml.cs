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

namespace CommonControls.Editors.AnimationBin
{
    /// <summary>
    /// Interaction logic for ExportBinWindow.xaml
    /// </summary>
    public partial class ExportBinWindow : Window
    {
        public string AnimPackName { get; private set; } = "Custom_table.animpack";
        public string BinName { get; private set; } = "Custom_table.bin";

        public ExportBinWindow()
        {
            InitializeComponent();
            AnimPackNameText.Text = AnimPackName;
            BinNameText.Text = BinName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AnimPackName = AnimPackNameText.Text;
            BinName = BinNameText.Text;

            DialogResult = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
