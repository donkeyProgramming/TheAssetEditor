using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Scene
{
    public class SceneViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public SceneContainer ThaScene { get; set; } 

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }


        IPackFile _packFile;
        public IPackFile MainFile
        {
            get => _packFile;
            set
            {
                _packFile = value;
                SetCurrentPackFile(_packFile);
            }
        }

        public SceneViewModel()
        {
            DisplayName = "3d viewer";
            ThaScene = new SceneContainer();
        }


        public string Text { get; set; }

        public bool Save()
        {
            throw new NotImplementedException();
        }



        void SetCurrentPackFile(IPackFile packedFile)
        {
           

        }
    }
}
