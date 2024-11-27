using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommonControls.BaseDialogs;
using CommonControls.Editors.AnimationPack.Converters;
using CommunityToolkit.Mvvm.Input;
using Editors.Shared.Core.Services;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;
using Shared.Ui.Common;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimPackViewModel : NotifyPropertyChangedImpl, IEditorInterface, ISaveableEditor, IFileEditor
    {
        private readonly IPackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private ITextConverter _activeConverter;
        private readonly ApplicationSettingsService _appSettings;
        private readonly IFileSaveService _packFileSaveService;
 

        public string DisplayName { get; set; } = "Not set";

        PackFile _packFile;

        public FilterCollection<IAnimationPackFile> AnimationPackItems { get; set; }

        SimpleTextEditorViewModel _selectedItemViewModel;
        public SimpleTextEditorViewModel SelectedItemViewModel { get => _selectedItemViewModel; set => SetAndNotify(ref _selectedItemViewModel, value); }


        public ICommand RemoveCommand { get; set; }
        public ICommand RenameCommand { get; set; }
        public ICommand CopyFullPathCommand { get; set; }

        public AnimPackViewModel(IPackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, ApplicationSettingsService appSettings, IFileSaveService packFileSaveService)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _appSettings = appSettings;
            _packFileSaveService = packFileSaveService;
 
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
            DisplayName = animPack.FileName;
        }

        string GetAnimSetFileName()
        {
            var window = new TextInputWindow("Fragment name", "");
            if (window.ShowDialog() == true)
            {
                var filename = SaveUtility.EnsureEnding(window.TextValue, ".frg");
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
            else if (seletedFile is Shared.GameFormats.AnimationPack.AnimPackFileTypes.AnimationBin typedBin)
                _activeConverter = new AnimationBinFileToXmlConverter();
            else if (seletedFile is AnimationBinWh3 wh3Bin)
                _activeConverter = new AnimationBinWh3FileToXmlConverter(_skeletonAnimationLookUpHelper);

            if (seletedFile == null || _activeConverter == null || seletedFile.IsUnknownFile)
            {
                SelectedItemViewModel = new SimpleTextEditorViewModel();
                SelectedItemViewModel.SaveCommand = null;
                SelectedItemViewModel.TextEditor?.ShowLineNumbers(true);
                SelectedItemViewModel.TextEditor?.SetSyntaxHighlighting("XML");
                SelectedItemViewModel.Text = "";
                SelectedItemViewModel.ResetChangeLog();
            }
            else
            {
                SelectedItemViewModel = new SimpleTextEditorViewModel();
                SelectedItemViewModel.SaveCommand = new RelayCommand(() => SaveActiveFile());
                SelectedItemViewModel.TextEditor?.ShowLineNumbers(true);
                SelectedItemViewModel.TextEditor?.SetSyntaxHighlighting(_activeConverter.GetSyntaxType());
                SelectedItemViewModel.Text = _activeConverter.GetText(seletedFile.ToByteArray());
                SelectedItemViewModel.ResetChangeLog();
            }

        }
        public void Close() { }
        public bool HasUnsavedChanges { get; set; }

        public PackFile CurrentFile => _packFile;

        public bool SaveActiveFile()
        {
            if (_packFile == null)
            {
                MessageBox.Show("Can not save in this mode - Open the file normally");
                return false;
            }
            var converter = (AnimationBinWh3FileToXmlConverter)_activeConverter;
            converter.AnimPackToValidate = _packFile;

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
            if (_packFile == null)
            {
                MessageBox.Show("Can not save in this mode - Open the file normally");
                return false;
            }

            if (SelectedItemViewModel != null && SelectedItemViewModel.HasUnsavedChanges())
            {
                if (MessageBox.Show("Editor has unsaved changes.\nSave anyway?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            var newAnimPack = new AnimationPackFile(_pfs.GetFullPath(_packFile));

            foreach (var file in AnimationPackItems.PossibleValues)
                newAnimPack.AddFile(file);

            var savePath = _pfs.GetFullPath(_packFile);

            var result = _packFileSaveService.Save(savePath, AnimationPackSerializer.ConvertToBytes(newAnimPack), false);
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

       //public static void ShowPreviewWinodow(PackFile animationPackFile, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, string selectedFileName, ApplicationSettingsService applicationSettings)
       //{
       //    if (animationPackFile == null)
       //    {
       //        MessageBox.Show("Unable to resolve packfile");
       //        return;
       //    }
       //
       //    var controller = new AnimPackViewModel(pfs, skeletonAnimationLookUpHelper, applicationSettings);
       //    controller._packFile = animationPackFile;
       //    controller.Load();
       //
       //    var containingWindow = new Window();
       //    containingWindow.Title = animationPackFile.Name;
       //
       //
       //    containingWindow.DataContext = controller;
       //    containingWindow.Content = new AnimationPackView();
       //
       //    containingWindow.Width = 1200;
       //    containingWindow.Height = 1100;
       //
       //
       //    containingWindow.Loaded += (sender, e) => controller.SetSelectedFile(selectedFileName);
       //
       //    containingWindow.ShowDialog();
       //}

        public void LoadFile(PackFile file)
        {
            _packFile = file;
            Load();
        }
    }
}
