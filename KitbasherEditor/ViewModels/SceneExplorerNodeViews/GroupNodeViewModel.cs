using Common;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class GroupNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        GroupNode _node;

        public string GroupName { get => _node.Name; set { _node.Name = value; NotifyPropertyChanged(); } }


        bool _saveAsFileEnabled;
        public bool SaveAsFileEnabled { get => _saveAsFileEnabled; set => SetAndNotify(ref _saveAsFileEnabled, value); }

        string _saveAsFilePath;
        public string SaveAsFilePath { get => _saveAsFilePath; set => SetAndNotify(ref _saveAsFilePath, value); }

        public GroupNodeViewModel(GroupNode node)
        {
            _node = node;
        }

        public void Dispose()
        {
        }
    }
}
