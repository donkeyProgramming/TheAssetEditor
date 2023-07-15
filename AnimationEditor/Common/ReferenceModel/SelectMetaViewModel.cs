using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System.Collections.ObjectModel;
using System.Linq;
using static CommonControls.FilterDialog.FilterUserControl;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectMetaViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        private readonly AssetViewModelBuilder _assetViewModelEditor;
        AssetViewModel _data;

        ObservableCollection<PackFile> _metaList = new ObservableCollection<PackFile>();
        public ObservableCollection<PackFile> MetaFiles { get { return _metaList; } set { SetAndNotify(ref _metaList, value); } }


        PackFile _selectedMetaFiles;
        public PackFile SelectedMetaFile { get => _selectedMetaFiles; set { SetAndNotify(ref _selectedMetaFiles, value); _assetViewModelEditor.SetMetaFile(_data, _selectedMetaFiles, _selectedPersistMeta); } }

        PackFile _selectedPersistMeta;
        public PackFile SelectedPersistMetaFile { get => _selectedPersistMeta; set { SetAndNotify(ref _selectedPersistMeta, value); _assetViewModelEditor.SetMetaFile(_data, _selectedMetaFiles, _selectedPersistMeta); } }


        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        public SelectMetaViewModel(AssetViewModelBuilder assetViewModelEditor, AssetViewModel data, PackFileService pfs)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _data = data;
            _pfs = pfs;

            // One skeleton change and anim chaange, clear
            Refresh();
        }

        public void Refresh()
        {
            // usually they end with .anim.meta but a few are just .meta, skip sound files (.snd.meta)
            var files = _pfs.FindAllWithExtention(".meta").Where(x => !x.Name.Contains(".snd."));
            MetaFiles = new ObservableCollection<PackFile>(files);
        }
    }
}
