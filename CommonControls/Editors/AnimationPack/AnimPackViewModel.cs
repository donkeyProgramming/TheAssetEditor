using CommonControls.BaseDialogs;
using CommonControls.Common;
using CommonControls.Editors.AnimationPack.Converters;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Windows;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimPackViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        AnimationPackFile _animPack;
        ITextConverter _activeConverter;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("");

        PackFile _packFile;
        public PackFile MainFile { get => _packFile; set { _packFile = value; Load(AnimationPackSerializer.Load(_packFile, _pfs)); } }

        public FilterCollection<IAnimationPackFile> AnimationPackItems { get; set; }

        SimpleTextEditorViewModel _selectedItemViewModel;
        public SimpleTextEditorViewModel SelectedItemViewModel { get => _selectedItemViewModel; set => SetAndNotify(ref _selectedItemViewModel, value); }


        public AnimPackViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
        }

        public void Load(AnimationPackFile animPack)
        {
            _animPack = animPack;
            var itemNames = animPack.Files.ToList();

            AnimationPackItems = new FilterCollection<IAnimationPackFile>(itemNames, ItemSelected, BeforeItemSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; }
            };

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

        public void CreateEmptyWarhammerAnimSetFile()
        {
            var fileName = GetAnimSetFileName();
            if (fileName == null)
                return;

            var animSet = AnimationPackFile.CreateExampleWarhammerAnimSet(fileName);
            AnimationPackItems.PossibleValues.Add(animSet);
            AnimationPackItems.UpdatePossibleValues(AnimationPackItems.PossibleValues);
        }


        public void CreateEmpty3kAnimSetFile()
        {
            var fileName = GetAnimSetFileName();
            if (fileName == null)
                return;

            var animSet = AnimationPackFile.CreateExample3kAnimSet(fileName);
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
            if (seletedFile is AnimationSetFile typedFragment)
                _activeConverter = new AnimationSetFileToXmlConverter(_skeletonAnimationLookUpHelper);
            else if (seletedFile is AnimationDbFile typedBin)
                _activeConverter = new AnimationDbFileToXmlConverter();
            else if (seletedFile is AnimationSet3kFile animSet3k)
                _activeConverter = new AnimationSet3kFileToXmlConverter(_skeletonAnimationLookUpHelper);

            if (seletedFile == null || _activeConverter == null || seletedFile.IsUnknownFile)
            {
                SelectedItemViewModel = new SimpleTextEditorViewModel();
                SelectedItemViewModel.SaveCommand = null;
                SelectedItemViewModel.TextEditor.ShowLineNumbers(true);
                SelectedItemViewModel.TextEditor.SetSyntaxHighlighting("XML");
                SelectedItemViewModel.Text = "";
                SelectedItemViewModel.ResetChangeLog();
                return;
            }

            SelectedItemViewModel = new SimpleTextEditorViewModel();
            SelectedItemViewModel.SaveCommand = new RelayCommand(() => SaveActiveFile());
            SelectedItemViewModel.TextEditor.ShowLineNumbers(true);
            SelectedItemViewModel.TextEditor.SetSyntaxHighlighting(_activeConverter.GetSyntaxType());
            SelectedItemViewModel.Text = _activeConverter.GetText(seletedFile.ToByteArray());
            SelectedItemViewModel.ResetChangeLog();
        }
        public void Close() { }
        public bool HasUnsavedChanges() => false;

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
            newAnimPack.FileName = _animPack.FileName;

            foreach (var file in AnimationPackItems.PossibleValues)
                newAnimPack.AddFile(file);

            var savePath = _pfs.GetFullPath(MainFile);

            SaveHelper.Save(_pfs, savePath, null, AnimationPackSerializer.ConvertToBytes(newAnimPack));
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

            return true;
        }

        public static void ShowPreviewWinodow(AnimationPackFile animationPackFile, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, string selectedFileName)
        {
            var controller = new AnimPackViewModel(pfs, skeletonAnimationLookUpHelper);
            controller.Load(animationPackFile);

            var containingWindow = new Window();
            containingWindow.Title = animationPackFile.FileName;


            containingWindow.DataContext = controller;
            containingWindow.Content = new AnimationPackView();

            containingWindow.Width = 1200;
            containingWindow.Height = 1100;


            containingWindow.Loaded += (sender, e) => controller.SetSelectedFile(selectedFileName);

            containingWindow.ShowDialog();
        }
    }
}
