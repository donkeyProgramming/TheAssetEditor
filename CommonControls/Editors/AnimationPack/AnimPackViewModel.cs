using Common;
using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.Services;
using CommonControls.Simple;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimPackViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {

        PackFileService _pfs;

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        PackFile _packFile;
        public IPackFile MainFile { get => _packFile; set { _packFile = value as PackFile; Load(_packFile); } }


        public FilterCollection<string> AnimationPackItems { get; set; }

        SimpleTextEditorViewModel _selectedItemViewModel;
        public SimpleTextEditorViewModel SelectedItemViewModel { get => _selectedItemViewModel; set => SetAndNotify(ref _selectedItemViewModel, value); }

        AnimationPackFile _animPack;

        public ICommand CreateNewFragmentCommand { get; set; }
        public ICommand CreateMatchedBinCommand { get; set; }
        public ICommand DeleteSelectedCommand { get; set; }

        public AnimPackViewModel(PackFileService pfs)
        {
            _pfs = pfs;

            CreateNewFragmentCommand = new RelayCommand(CreateFragment);
            //CreateMatchedBinCommand = new RelayCommand(CreateFragment);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected);
        }

        void CreateFragment()
        {
            var window = new TextInputWindow("Fragment name", "");
            if (window.ShowDialog() == true)
            {
                var filename = SaveHelper.EnsureEnding(window.TextValue, ".frg");
                var filePath = @"animations/animation_tables/" + filename;

                if (!SaveHelper.IsFilenameUnique(_pfs, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                var fragment = new AnimationFragment(filePath);

                fragment.Skeletons = new AnimationFragment.StringArrayTable("ExampleSkeleton", "ExampleSkeleton");
                fragment.Fragments.Add(new AnimationFragmentEntry()
                {
                    AnimationFile = @"animations/battle/ExampleSkeleton/exampleanim.anim",
                    MetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim.anm.meta",
                    Skeleton = @"ExampleSkeleton",
                    Slot = AnimationSlotTypeHelper.GetfromValue("MISSING_ANIM"),
                    SoundMetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim.snd.meta"
                });

                fragment.Fragments.Add(new AnimationFragmentEntry()
                {
                    AnimationFile = @"animations/battle/ExampleSkeleton/exampleanim2.anim",
                    MetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim2.anm.meta",
                    Skeleton = @"ExampleSkeleton",
                    Slot = AnimationSlotTypeHelper.GetfromValue("STAND"),
                    SoundMetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim2.snd.meta"
                });

                fragment.UpdateMinAndMaxSlotIds();
                _animPack.Fragments.Add(fragment);

                AnimationPackItems.PossibleValues.Add(filePath);
                AnimationPackItems.UpdatePossibleValues(AnimationPackItems.PossibleValues);
            }
        }

        void DeleteSelected()
        { }

        private void Load(PackFile packFile)
        {
            _animPack = new AnimationPackFile(packFile);
            var itemNames = new List<string>();

            if (_animPack.AnimationBin != null)
                itemNames.Add(_animPack.AnimationBin.FileName);

            foreach (var item in _animPack.Fragments)
                itemNames.Add(item.FileName);

            AnimationPackItems = new FilterCollection<string>(itemNames, ItemSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value).Success; }
            };

            DisplayName = _packFile.Name;
        }

        ITextConverter GetConverterForType(AnimationPackFile.AnimationPackFileType type)
        { 
            switch(type)
            {
                case AnimationPackFile.AnimationPackFileType.Bin:
                    return new AnimationBinToXmlConverter();
                case AnimationPackFile.AnimationPackFileType.Fragment:
                    return new AnimationFragmentToXmlConverter();
            }

            return new DefaultTextConverter();
        }

        void ItemSelected(string item)
        {
            if (SelectedItemViewModel != null && SelectedItemViewModel.HasUnsavedChanges())
            { 
                // Are you sure?
            }

            if (string.IsNullOrWhiteSpace(item) == false)
            {
                var bytes = _animPack.GetFile(item, out var animPackFileType);

                ITextConverter converter = GetConverterForType(animPackFileType);
                ICommand saveCommand = null;
                if (animPackFileType == AnimationPackFile.AnimationPackFileType.Bin)
                {
                    saveCommand = new RelayCommand(() => SaveBin());
                }
                else if (animPackFileType == AnimationPackFile.AnimationPackFileType.Fragment)
                {
                    saveCommand = new RelayCommand(() => SaveBin());
                }

                SelectedItemViewModel = new SimpleTextEditorViewModel();
                SelectedItemViewModel.SaveCommand = saveCommand;
                SelectedItemViewModel.TextEditor.ShowLineNumbers(true);
                SelectedItemViewModel.TextEditor.SetSyntaxHighlighting(converter.GetSyntaxType());
                SelectedItemViewModel.Text = converter.GetText(bytes);
                SelectedItemViewModel.ResetChangeLog();
            }
        }

        bool SaveBin()
        {
            var fileName = AnimationPackItems.SelectedItem;
            _animPack.GetFile(fileName, out var animPackFileType);

            ITextConverter converter = GetConverterForType(animPackFileType);
            var bytes = converter.ToBytes(SelectedItemViewModel.Text, fileName, null, out var error);

           if (bytes == null || error != null)
           {
               SelectedItemViewModel.TextEditor.HightLightText(error.ErrorLineNumber, error.ErrorPosition, error.ErrorLength);
               MessageBox.Show(error.Text, "Error");
                return false;
           }
           
           _animPack.UpdateFileFromBytes(fileName, bytes);
            return true;
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
            var savePath = _pfs.GetFullPath(MainFile as PackFile);
            SaveHelper.Save(_pfs, savePath, null, _animPack.ToByteArray());
            return true;
        }
    }
}
