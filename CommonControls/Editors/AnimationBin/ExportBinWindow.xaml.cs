// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;

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
