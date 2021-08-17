using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.DB;
using FileTypes.MetaData;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MainEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pf;
        SchemaManager _schemaManager;
        MetaDataFile _metaDataFile;

        public MetaDataTagEditorViewModel Editor { get; set; }
        public ICommand SaveCommand { get; set; }

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        IPackFile _file;
        public IPackFile MainFile { get => _file; set => Initialise(value); }


        public MetaDataViewModel __metaDataFileViewModel;
        public MetaDataViewModel MetaDataFile { get => __metaDataFileViewModel; set => SetAndNotify(ref __metaDataFileViewModel, value); }

        public MainEditorViewModel(PackFileService pf, SchemaManager schemaManager)
        {
            _pf = pf;
            _schemaManager = schemaManager;
            SaveCommand = new RelayCommand(() => Save());
        }

        void Initialise(IPackFile file)
        {
            _file = file;
            DisplayName = file.Name;


            var fileName = _pf.GetFullPath(_file as PackFile);
            var fileContent = (_file as PackFile).DataSource.ReadData();
            _metaDataFile = MetaDataFileParser.ParseFile(fileContent, fileName);
            _metaDataFile.Validate(_schemaManager);

            MetaDataFile = new MetaDataViewModel(_metaDataFile, _schemaManager);
            Editor = new MetaDataTagEditorViewModel(_schemaManager, MetaDataFile);
        }

        public void Close()
        {
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }

        public bool Save()
        {
            var path = _pf.GetFullPath(_file as PackFile);

            if (!MetaDataFile.IsValid(out string errorMessage))
            {
                MessageBox.Show($"Unable to save : {errorMessage}");
                return false;
            }

            var bytes = MetaDataFile.GetBytes();
            var res = SaveHelper.Save(_pf, path, null, bytes);
            if (res != null)
            {
                _file = res;
                DisplayName = _file.Name;
            }
            return _file != null;
        }
    }
}

