using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    public class ContextMenuItem
    {
        public string Name { get; set; }
        public ICommand? Command { get; set; }
        public ObservableCollection<ContextMenuItem> ContextMenu { get; set; } = [];

        public ContextMenuItem(string name = "", ICommand? command = null)
        {
            Name = name;
            Command = command;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
