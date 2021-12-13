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
        public PackFile MainFile { get => _packFile; set { _packFile = value; Load(AnimationPackSerializer.Load(_packFile)); } }

        public FilterCollection<string> AnimationPackItems { get; set; }

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
            var itemNames = animPack.Files.Select(X => X.FileName).ToList();

            AnimationPackItems = new FilterCollection<string>(itemNames, ItemSelected, BeforeItemSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value).Success; }
            };

            DisplayName.Value = animPack.FileName;
        }

        public void CreateEmptyAnimSetFile()
        {
            //var window = new TextInputWindow("Fragment name", "");
            //if (window.ShowDialog() == true)
            //{
            //    var filename = SaveHelper.EnsureEnding(window.TextValue, ".frg");
            //    var filePath = @"animations/animation_tables/" + filename;
            //
            //    if (!SaveHelper.IsFilenameUnique(_pfs, filePath))
            //    {
            //        MessageBox.Show("Filename is not unique");
            //        return;
            //    }
            //
            //    var fragment = new AnimationFragment(filePath);
            //
            //    fragment.Skeletons = new AnimationFragment.StringArrayTable("ExampleSkeleton", "ExampleSkeleton");
            //    fragment.Fragments.Add(new AnimationFragmentEntry()
            //    {
            //        AnimationFile = @"animations/battle/ExampleSkeleton/exampleanim.anim",
            //        MetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim.anm.meta",
            //        Skeleton = @"ExampleSkeleton",
            //        Slot = AnimationSlotTypeHelper.GetfromValue("MISSING_ANIM"),
            //        SoundMetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim.snd.meta"
            //    });
            //
            //    fragment.Fragments.Add(new AnimationFragmentEntry()
            //    {
            //        AnimationFile = @"animations/battle/ExampleSkeleton/exampleanim2.anim",
            //        MetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim2.anm.meta",
            //        Skeleton = @"ExampleSkeleton",
            //        Slot = AnimationSlotTypeHelper.GetfromValue("STAND"),
            //        SoundMetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim2.snd.meta"
            //    });
            //
            //    //fragment.UpdateMinAndMaxSlotIds();
            //    _animPack.Fragments.Add(fragment);
            //
            //    AnimationPackItems.PossibleValues.Add(filePath);
            //    AnimationPackItems.UpdatePossibleValues(AnimationPackItems.PossibleValues);
            //}
        }




        public void SetSelectedFile(string path)
        {
            AnimationPackItems.SelectedItem = AnimationPackItems.PossibleValues.FirstOrDefault(x => x == path);
        }

        //ITextConverter GetConverterForType(AnimationPackFile.AnimationPackFileType type)
        //{
        //    switch (type)
        //    {
        //        case AnimationPackFile.AnimationPackFileType.Bin:
        //            return new AnimationBinToXmlConverter();
        //        case AnimationPackFile.AnimationPackFileType.Fragment:
        //            return new AnimationFragmentToXmlConverter(_skeletonAnimationLookUpHelper);
        //    }
        //
        //    return new DefaultTextConverter();
        //}
        //
        bool BeforeItemSelected(string item)
        {
            if (SelectedItemViewModel != null && SelectedItemViewModel.HasUnsavedChanges())
            {
                if (MessageBox.Show("Editor has unsaved changes that will be lost.\nContinue?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            return true;
        }

   
        void ItemSelected(string item)
        {
            if (string.IsNullOrWhiteSpace(item) == false)
            {
                var seletedFile = _animPack.Files.FirstOrDefault(x => x.FileName == item);
                if (seletedFile == null)
                    return;

                if (seletedFile is AnimationSetFile typedFragment)
                    _activeConverter = new AnimationSetFileToXmlConverter(_skeletonAnimationLookUpHelper);
                else if (seletedFile is AnimationDbFile typedBin)
                    _activeConverter = new AnimationDbFileToXmlConverter();

                SelectedItemViewModel = new SimpleTextEditorViewModel();
                SelectedItemViewModel.SaveCommand = new RelayCommand(() => SaveActiveFile());
                SelectedItemViewModel.TextEditor.ShowLineNumbers(true);
                SelectedItemViewModel.TextEditor.SetSyntaxHighlighting(_activeConverter.GetSyntaxType());
                SelectedItemViewModel.Text = _activeConverter.GetText(seletedFile.ToByteArray());
                SelectedItemViewModel.ResetChangeLog();
            }
        }
      
        public bool SaveActiveFile()
        {
            if (MainFile == null)
            {
                MessageBox.Show("Can not save in this mode - Open the file normally");
                return false;
            }

            var fileName = AnimationPackItems.SelectedItem;
            var bytes = _activeConverter.ToBytes(SelectedItemViewModel.Text, fileName, _pfs, out var error);

            if (bytes == null || error != null)
            {
                SelectedItemViewModel.TextEditor.HightLightText(error.ErrorLineNumber, error.ErrorPosition, error.ErrorLength);
                MessageBox.Show(error.Text, "Error");
                return false;
            }

            var seletedFile = _animPack.Files.FirstOrDefault(x => x.FileName == fileName);
            seletedFile.CreateFromBytes(bytes);

            SelectedItemViewModel.ResetChangeLog();

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
           
           var savePath = _pfs.GetFullPath(MainFile );
           SaveHelper.Save(_pfs, savePath, null, AnimationPackSerializer.ConvertToBytes(_animPack));
            return true;
        }

        public bool ViewSelectedAsTable()
        {
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
            //var controller = new AnimPackViewModel(pfs, skeletonAnimationLookUpHelper);
            //controller.Load(animationPack);
            //
            //var containingWindow = new Window();
            //containingWindow.Title = "Animation pack - " + animationPack.FileName;
            //
            //containingWindow.DataContext = controller;
            //containingWindow.Content = new CommonControls.Editors.AnimationPack.AnimationPackView();
            //
            //containingWindow.Width = 1200;
            //containingWindow.Height = 1100;
            //
            //
            //containingWindow.Loaded += (sender, e) => controller.SetSelectedFile(selectedFileName);
            //
            //containingWindow.ShowDialog();
        }
    }
}
