using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu
{
    public class ContextMenuItem
    {
        public string Name { get; set; }
        public ICommand? Command { get; set; }
        public ObservableCollection<ContextMenuItem?> ContextMenu { get; set; } = [];

        public ContextMenuItem(string name, Action? action)
        {
            Name = name;
            if (action != null)
                Command = new RelayCommand(action);
        }

        public override string ToString() => Name;
    }
}
