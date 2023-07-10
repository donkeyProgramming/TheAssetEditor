// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows;
using CommonControls.Common;

namespace CommonControls.BaseDialogs.ToolSelector
{
    /// <summary>
    /// Interaction logic for ToolSelectorWindow.xaml
    /// </summary>
    public partial class ToolSelectorWindow : Window
    {

        public ToolSelectorWindow()
        {
            InitializeComponent();
        }

        public static EditorEnums CreateAndShow(IEnumerable<EditorEnums> editors)
        {
            var window = new ToolSelectorWindow() { Owner = Application.Current.MainWindow };

            foreach (var tool in editors)
                window.PossibleToolsComboBox.Items.Add(tool);

            window.PossibleToolsComboBox.Items.Add(EditorEnums.None);

            if (window.PossibleToolsComboBox.Items.Count != 0)
                window.PossibleToolsComboBox.SelectedItem = window.PossibleToolsComboBox.Items[0];

            var result = window.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (window.PossibleToolsComboBox.SelectedItem == null)
                    return EditorEnums.None;

                return (EditorEnums)window.PossibleToolsComboBox.SelectedItem;
            }

            return EditorEnums.None;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
