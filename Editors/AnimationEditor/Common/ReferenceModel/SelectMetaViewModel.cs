using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System.Collections.ObjectModel;
using System.Linq;
using static CommonControls.FilterDialog.FilterUserControl;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectMetaViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _pfs;
        private readonly SceneObjectBuilder _assetViewModelEditor;
        private readonly SceneObject _data;

        ObservableCollection<PackFile> _metaList = new();
        public ObservableCollection<PackFile> MetaFiles { get { return _metaList; } set { SetAndNotify(ref _metaList, value); } }

        public PackFile SelectedMetaFile 
        {
            get => _data.MetaData; 
            set { _assetViewModelEditor.SetMetaFile(_data, value, _data.PersistMetaData); RefreshMetaDataElements(); } 
        }

        public PackFile SelectedPersistMetaFile 
        { 
            get => _data.PersistMetaData; 
            set { _assetViewModelEditor.SetMetaFile(_data, _data.MetaData, value); RefreshMetaDataElements(); }
        }

        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        public SelectMetaViewModel(SceneObjectBuilder assetViewModelEditor, SceneObject data, PackFileService pfs)
        {
            _assetViewModelEditor = assetViewModelEditor;
            
            _data = data;
            _pfs = pfs;

            _data.MetaDataChanged += (x) => RefreshMetaDataElements();

            
            var files = _pfs.FindAllWithExtention(".meta").Where(x => !x.Name.Contains(".snd."));
            MetaFiles = new ObservableCollection<PackFile>(files);
        }

        void RefreshMetaDataElements()
        {
            NotifyPropertyChanged(nameof(SelectedMetaFile));
            NotifyPropertyChanged(nameof(SelectedPersistMetaFile));
        }

    }
}
