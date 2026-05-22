using System.Collections.Generic;
using System.Windows;
using CommonControls.BaseDialogs.ToolSelector;
using Shared.Core.ToolCreation;

namespace Shared.Ui.BaseDialogs.ToolSelector
{
    public class ToolSelectorUiProvider : IToolSelectorUiProvider
    {
        public EditorEnums CreateAndShow(IEnumerable<EditorEnums> editors)
        {
            var window = new ToolSelectorWindow() { Owner = Application.Current.MainWindow };

            foreach (var tool in editors)
                window.PossibleTools.Items.Add(tool);

            window.PossibleTools.Items.Add(EditorEnums.None);

            if (window.PossibleTools.Items.Count != 0)
                window.PossibleTools.SelectedItem = window.PossibleTools.Items[0];

            var result = window.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (window.PossibleTools.SelectedItem == null)
                    return EditorEnums.None;

                return (EditorEnums)window.PossibleTools.SelectedItem;
            }

            return EditorEnums.None;
        }
    }
}
