using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Twui.Editor.Datatypes
{
    public partial class TwuiFile : ObservableObject
    {
        [ObservableProperty] FileMetaData _fileMetaData = new FileMetaData();
        [ObservableProperty] ObservableCollection<Component> _components  = [];
        [ObservableProperty] Hierarchy _hierarchy  = new Hierarchy();
    }
}
