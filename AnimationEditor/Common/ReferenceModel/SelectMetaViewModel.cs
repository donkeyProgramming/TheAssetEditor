using Common;
using CommonControls.Services;
using FileTypes.MetaData;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static CommonControls.FilterDialog.FilterUserControl;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectMetaViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _data;

        ObservableCollection<PackFile> _metaList = new ObservableCollection<PackFile>();
        public ObservableCollection<PackFile> MetaFiles { get { return _metaList; } set { SetAndNotify(ref _metaList, value); } }


        PackFile _selectedMetaFiles;
        public PackFile SelectedMetaFile { get => _selectedMetaFiles; set { SetAndNotify(ref _selectedMetaFiles, value); _data.SetMetaFile(LoadMetaDataFile(value)); } }

        PackFile _selectedPersistMeta;
        public PackFile SelectedPersistMetaFile { get => _selectedPersistMeta; set { SetAndNotify(ref _selectedPersistMeta, value); _data.SetPersistantMetaFile(LoadMetaDataFile(value)); } } 


        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        public SelectMetaViewModel(AssetViewModel data, PackFileService pfs)
        {
            _data = data;
            _pfs = pfs;

            // One skeleton change and anim chaange, clear

            var files = pfs.FindAllWithExtention(".meta").Where(x=>x.Name.Contains("anm.meta"));
            MetaFiles = new ObservableCollection<PackFile>(files);
        }

        private void SkeletonChanged(string selectedSkeletonPath)
        {
        }   

        private void AnimationChanged(AnimationReference animationReference)
        {
        }  


        MetaDataFile LoadMetaDataFile(PackFile value)
        {
            if (value == null)
                return null;

            var fileName = _pfs.GetFullPath(value);
            var fileContent = value.DataSource.ReadData();
            var metaDataFile = MetaDataFileParser.ParseFile(fileContent, fileName);
            return metaDataFile;
        }
    }
}
