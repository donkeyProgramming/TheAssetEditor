using System.Collections.Generic;
using System.Windows;
using SharedCore.ToolCreation;

namespace CommonControls.BaseDialogs.ToolSelector
{
    public class ToolSelectorUiProvider : IToolSelectorUiProvider
    {
        public EditorEnums CreateAndShow(IEnumerable<EditorEnums> editors)
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
    }
}
