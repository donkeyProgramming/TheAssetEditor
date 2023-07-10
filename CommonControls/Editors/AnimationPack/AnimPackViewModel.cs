// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommonControls.BaseDialogs;
using CommonControls.Common;
using CommonControls.Editors.AnimationPack.Converters;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimPackViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        ITextConverter _activeConverter;
        ApplicationSettingsService _appSettings;
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("");

        PackFile _packFile;
        public PackFile MainFile { get => _packFile; set { _packFile = value; Load(); } }

        public FilterCollection<IAnimationPackFile> AnimationPackItems { get; set; }

        SimpleTextEditorViewModel _selectedItemViewModel;
        public SimpleTextEditorViewModel SelectedItemViewModel { get => _selectedItemViewModel; set => SetAndNotify(ref _selectedItemViewModel, value); }


        public ICommand RemoveCommand { get; set; }
        public ICommand RenameCommand { get; set; }
        public ICommand CopyFullPathCommand { get; set; }

        public AnimPackViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, ApplicationSettingsService appSettings)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _appSettings = appSettings;

            AnimationPackItems = new FilterCollection<IAnimationPackFile>(new List<IAnimationPackFile>(), ItemSelected, BeforeItemSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; }
            };

            RemoveCommand = new RelayCommand(Remove);
            RenameCommand = new RelayCommand(Rename);
            CopyFullPathCommand = new RelayCommand(CopyFullPath);
        }

        private void Rename()
        {
            var animFile = AnimationPackItems.PossibleValues.FirstOrDefault(file => file == AnimationPackItems.SelectedItem);
            if (animFile == null)
                return;

            var window = new TextInputWindow("Rename Anim File", animFile.FileName);
            if (window.ShowDialog() == true)
                animFile.FileName = window.TextValue;

            // way to refresh the view
            AnimationPackItems.RefreshFilter();
        }

        private void Remove()
        {
            AnimationPackItems.PossibleValues.Remove(AnimationPackItems.SelectedItem);

            // way to refresh the view
            AnimationPackItems.RefreshFilter();
        }

        private void CopyFullPath()
        {
            Clipboard.SetText(AnimationPackItems.SelectedItem.FileName);
        }

        public void Load()
        {
            var animPack = AnimationPackSerializer.Load(_packFile, _pfs);
            var itemNames = animPack.Files.ToList();
            AnimationPackItems.UpdatePossibleValues(itemNames);
            DisplayName.Value = animPack.FileName;
        }

        string GetAnimSetFileName()
        {
            var window = new TextInputWindow("Fragment name", "");
            if (window.ShowDialog() == true)
            {
                var filename = SaveHelper.EnsureEnding(window.TextValue, ".frg");
                return filename;
            }

            return null;
        }

        public void CreateEmptyWarhammer3AnimSetFile()
        {
            var fileName = GetAnimSetFileName();
            if (fileName == null)
                return;

            var animSet = AnimationPackSampleDataCreator.CreateExampleWarhammer3AnimSet(fileName);
            AnimationPackItems.PossibleValues.Add(animSet);
            AnimationPackItems.UpdatePossibleValues(AnimationPackItems.PossibleValues);
        }

        public void SetSelectedFile(string path)
        {
            AnimationPackItems.SelectedItem = AnimationPackItems.PossibleValues.FirstOrDefault(x => x.FileName == path);
        }

        bool BeforeItemSelected(IAnimationPackFile item)
        {
            if (SelectedItemViewModel != null && SelectedItemViewModel.HasUnsavedChanges())
            {
                if (MessageBox.Show("Editor has unsaved changes that will be lost.\nContinue?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            return true;
        }

        void ItemSelected(IAnimationPackFile seletedFile)
        {
            _activeConverter = null;
            if (seletedFile is AnimationFragmentFile typedFragment)
                _activeConverter = new AnimationFragmentFileToXmlConverter(_skeletonAnimationLookUpHelper, _appSettings.CurrentSettings.CurrentGame);
            else if (seletedFile is FileTypes.AnimationPack.AnimPackFileTypes.AnimationBin typedBin)
                _activeConverter = new AnimationBinFileToXmlConverter();
            else if (seletedFile is FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3 wh3Bin)
                _activeConverter = new AnimationBinWh3FileToXmlConverter(_skeletonAnimationLookUpHelper);

            if (seletedFile == null || _activeConverter == null || seletedFile.IsUnknownFile)
            {
                SelectedItemViewModel = new SimpleTextEditorViewModel();
                SelectedItemViewModel.SaveCommand = null;
                SelectedItemViewModel.TextEditor?.ShowLineNumbers(true);
                SelectedItemViewModel.TextEditor?.SetSyntaxHighlighting("XML");
                SelectedItemViewModel.Text = "";
                SelectedItemViewModel.ResetChangeLog();
                return;
            }

            SelectedItemViewModel = new SimpleTextEditorViewModel();
            SelectedItemViewModel.SaveCommand = new RelayCommand(() => SaveActiveFile());
            SelectedItemViewModel.TextEditor?.ShowLineNumbers(true);
            SelectedItemViewModel.TextEditor?.SetSyntaxHighlighting(_activeConverter.GetSyntaxType());
            SelectedItemViewModel.Text = _activeConverter.GetText(seletedFile.ToByteArray());
            SelectedItemViewModel.ResetChangeLog();
        }
        public void Close() { }
        public bool HasUnsavedChanges { get; set; }

        public bool SaveActiveFile()
        {
            if (MainFile == null)
            {
                MessageBox.Show("Can not save in this mode - Open the file normally");
                return false;
            }

            var fileName = AnimationPackItems.SelectedItem.FileName;
            var bytes = _activeConverter.ToBytes(SelectedItemViewModel.Text, fileName, _pfs, out var error);

            if (bytes == null || error != null)
            {
                SelectedItemViewModel.TextEditor.HightLightText(error.ErrorLineNumber, error.ErrorPosition, error.ErrorLength);
                MessageBox.Show(error.Text, "Error");
                return false;
            }

            var seletedFile = AnimationPackItems.SelectedItem;
            seletedFile.CreateFromBytes(bytes);
            seletedFile.IsChanged.Value = true;

            SelectedItemViewModel.ResetChangeLog();
            HasUnsavedChanges = true;


            return true;
        }


        public bool Save()
        {
            if (MainFile == null)
            {
                MessageBox.Show("Can not save in this mode - Open the file normally");
                return false;
            }

            if (SelectedItemViewModel != null && SelectedItemViewModel.HasUnsavedChanges())
            {
                if (MessageBox.Show("Editor has unsaved changes.\nSave anyway?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            var newAnimPack = new AnimationPackFile();
            newAnimPack.FileName = _pfs.GetFullPath(MainFile);

            foreach (var file in AnimationPackItems.PossibleValues)
                newAnimPack.AddFile(file);

            var savePath = _pfs.GetFullPath(MainFile);

            var result = SaveHelper.Save(_pfs, savePath, null, AnimationPackSerializer.ConvertToBytes(newAnimPack));
            if (result != null)
            {
                HasUnsavedChanges = false;
                foreach (var file in AnimationPackItems.PossibleValues)
                    file.IsChanged.Value = false;
            }

            return true;
        }

        public bool ViewSelectedAsTable()
        {
            throw new System.Exception("TODO");
            //var selectedItem = AnimationPackItems.SelectedItem;
            //if (selectedItem == null)
            //    return false;
            //
            //if (_animPack.GetFileType(selectedItem) == AnimationPackFile.AnimationPackFileType.Bin)
            //{
            //    var data = new ObservableCollection<AnimationBinEntry>();
            //
            //    var bin = _animPack.GetAnimBin(selectedItem);
            //    foreach (var item in bin.AnimationTableEntries)
            //        data.Add(item);
            //
            //    AnimTablePreviewWindow window = new AnimTablePreviewWindow()
            //    {
            //        DataContext = data
            //    };
            //
            //    window.ShowDialog();
            //}
            //else
            //{
            //    var data = new ObservableCollection<AnimationFragmentEntry>();
            //
            //    var frag = _animPack.GetAnimFragment(selectedItem);
            //    foreach (var item in frag.Fragments)
            //        data.Add(item);
            //
            //    AnimTablePreviewWindow window = new AnimTablePreviewWindow()
            //    {
            //        DataContext = data
            //    };
            //
            //    window.ShowDialog();
            //}
            //
            //return true;
        }

        public void ExportAnimationSlotsWh3Action()
        {
            var slots = AnimationSlotTypeHelperWh3.Values.Select(x => x.Id + "\t\t" + x.Value).ToList();
            SaveAnimationSlotsToFile(slots);
        }

        public void ExportAnimationSlotsWh2Action()
        {
            var slots = DefaultAnimationSlotTypeHelper.Values.Select(x => x.Id + "\t\t" + x.Value).ToList();
            SaveAnimationSlotsToFile(slots);
        }

        void SaveAnimationSlotsToFile(List<string> slots)
        {
            using var dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.Filter = "Text files(*.txt) | *.txt | All files(*.*) | *.* ";
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            string path = dlg.FileName;

            StringBuilder sb = new StringBuilder();
            foreach (var slot in slots)
                sb.AppendLine(slot);

            File.WriteAllText(path, sb.ToString());
        }

        public static void ShowPreviewWinodow(PackFile animationPackFile, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, string selectedFileName, ApplicationSettingsService applicationSettings)
        {
            if (animationPackFile == null)
            {
                MessageBox.Show("Unable to resolve packfile");
                return;
            }

            var controller = new AnimPackViewModel(pfs, skeletonAnimationLookUpHelper, applicationSettings);
            controller.MainFile = animationPackFile;
            controller.Load();

            var containingWindow = new Window();
            containingWindow.Title = animationPackFile.Name;


            containingWindow.DataContext = controller;
            containingWindow.Content = new AnimationPackView();

            containingWindow.Width = 1200;
            containingWindow.Height = 1100;


            containingWindow.Loaded += (sender, e) => controller.SetSelectedFile(selectedFileName);

            containingWindow.ShowDialog();
        }
    }
}
