using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu
{
    public class ContextMenuItem2
    {
        public string Name { get; set; }
        public ICommand? Command { get; set; }
        public ObservableCollection<ContextMenuItem2?> ContextMenu { get; set; } = [];

        public ContextMenuItem2(string name, Action? action)
        {
            Name = name;
            if (action != null)
                Command = new RelayCommand(action);
        }

        public override string ToString() => Name;
    }


}
