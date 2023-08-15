using CommonControls.Common;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using CommonControls.Services;
using CommonControls.FileTypes.PackFiles.Models;
using System;
using System.IO;
using System.Linq;
using CommonControls.FileTypes.Animation;
using AssetManagement.Strategies.Fbx.ImportDialog.DataModels;
using AssetManagement.Strategies.Fbx.ImportDialog.Views;
// TODO: clean "using"s (EVERYWHERE, not just here)


// finish "rationaliztion" this ModelView, around the concept of "FileInfoData" as input
namespace AssetManagement.Strategies.Fbx.ImportDialog.ViewModels
{

    // TODO: not needed anymore, when the DataGrid is not longer used
    //public class FileInfoItem
    //{
    //    public FileInfoItem() { }
    //    public FileInfoItem(string name, string value)
    //    {
    //        Name = name;
    //        Value = value;
    //    }

    //    public string Name { get; set; }
    //    public string Value { get; set; }
    //}

    /// <summary>
    /// Elementsfor the Skeleeto Select ComboBox
    /// </summary>
    public class SkeletonElement : NotifyPropertyChangedImpl
    {
        public string Name
        {
            get
            {

                return SkeletonPackFile != null ? SkeletonPackFile.Name : "{No Skeleton}";
            }

            set { }
        }

        public override string ToString()
        {
            return SkeletonPackFile != null ? SkeletonPackFile.Name : "{No Skeleton}";
        }

        /// <summary>
        /// Creates a "No skeleton" element, for insertion into combobox
        /// </summary>
        /// <returns></returns>
        public static SkeletonElement GetNoSkeletonElement()
        {
            return new SkeletonElement() { SkeletonPackFile = null };
        }

        public string Value { get; set; } = null; // this member is not really needed
        public PackFile SkeletonPackFile { get; set; } = null; // null means "no skeleton", and if the users selects this, the model will not be rigged        
    }

    public class FBXSettingsViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _packFileService;
        private FbxSettingsModel _fbxSettings; // Maybe "FileInfoDat" should store only, and FBXSettings is for "get info from dialog", not something that already contains data, or not?

        // TODO: not needed anymore, when the DataGrid is not longer used
        //public ObservableCollection<FileInfoItem> FileInfoGridSource { get; set; } = new ObservableCollection<FileInfoItem>();

        // TODO: REMOVE if any of it not needed?
        //public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>();

        //private string _filename = "test Binding...";
        //public string FileName { get { return _filename; } set { _filename = value; NotifyPropertyChanged(); } }



        public string FileName { get { return _fbxSettings.FileInfoData.FileName; } set { _fbxSettings.FileInfoData.FileName = value; NotifyPropertyChanged(); } }
        public string SdkVersion
        {
            get { return $"{_fbxSettings.FileInfoData.SdkVersionUsed.X}.{_fbxSettings.FileInfoData.SdkVersionUsed.Y}.{_fbxSettings.FileInfoData.SdkVersionUsed.Z}"; }
            set {; }
        }
        public string SkeletonNodeName { get { return _fbxSettings.FileInfoData.SkeletonName; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string Units { get { return _fbxSettings.FileInfoData.Units; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MeshCount { get { return $"{_fbxSettings.FileInfoData.MeshCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string NodeCount { get { return $"{_fbxSettings.FileInfoData.ElementCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MaterialCount { get { return $"{_fbxSettings.FileInfoData.MaterialCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string AnimationCount { get { return $"Num{_fbxSettings.FileInfoData.AnimationsCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string BoneCount { get { return $"Num{_fbxSettings.FileInfoData.BoneCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }

        // TODO: remove?
        //public bool AnySkeletonSelected { get; set; } = false;   

        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<bool> UseAutoRigging { get; set; } = new NotifyAttr<bool>();

        // -- skeleton combox box
        public NotifyAttr<bool> ComboOpen { get; set; } = new NotifyAttr<bool>();

        private readonly ObservableCollection<SkeletonElement> _actualSkeletonList = new ObservableCollection<SkeletonElement>();

        //private ObservableCollection<SkeletonElement> _filteredSkeletonList = new ObservableCollection<SkeletonElement>();



        private ObservableCollection<SkeletonElement> _skeketonComboBoxContent = new ObservableCollection<SkeletonElement>();

        public ObservableCollection<SkeletonElement> BSkeketonComboBoxContent
        {
            get
            {
                return _skeketonComboBoxContent;
            }

            set
            {
                _skeketonComboBoxContent = value;
                SetAndNotify(ref _skeketonComboBoxContent, value);
            }
        }

        ObservableCollection<SkeletonElement> GetFilteredComboBoxContent(string filterString)
        {
            if (BSkeletonComboxSearchText == null || BSkeletonComboxSearchText == "") // where no string entered show the whole lists
            {
                return _actualSkeletonList;
            }


            var queryFilteredContent = _actualSkeletonList
                        .Where(x => x.Name.Contains(filterString, StringComparison.InvariantCultureIgnoreCase));

            // TODO: remove debugging code:
            var testList = queryFilteredContent.ToList();

            return new ObservableCollection<SkeletonElement>(queryFilteredContent);
        }

        private string _skeletonComboxSearchText;
        public string BSkeletonComboxSearchText
        {
            get { return _skeletonComboxSearchText; }
            set
            {
                if (value != null)
                {
                    _skeletonComboxSearchText = value;
                    NotifyPropertyChanged(nameof(BSkeletonComboxSearchText));

                    if (BSkeletonComboxSearchText != null && BSkeletonComboxSearchText.Any())
                    {
                        BSkeketonComboBoxContent = GetFilteredComboBoxContent(_skeletonComboxSearchText);
                        NotifyPropertyChanged(nameof(BSkeketonComboBoxContent));
                    }
                    else
                    {
                        BSkeketonComboBoxContent = _actualSkeletonList;
                        NotifyPropertyChanged(nameof(BSkeketonComboBoxContent));
                    }
                }
            }
        }



        //private string _skeletonComboxSearchText;
        //public NotifyAttr<string> BSkeletonComboxSearchText
        //{
        //    get
        //    {
        //        return new NotifyAttr<string>(_skeletonComboxSearchText);
        //    }

        //    set
        //    {
        //        _skeletonComboxSearchText = value.Value;
        //    }
        //    //{

        //    //    //_filteredSkeletonList = GetFilteredComboBoxContent(BSkeletonComboxSearchText);
        //    //    //BSkeketonComboBoxContent = new ObservableCollection<SkeletonElement>();
        //    //    //BSkeketonComboBoxContent = _filteredSkeletonList;
        //    //    _skeletonComboxSearchText = value;
        //    //    //SetAndNotify(ref _skeletonComboxSearchText, value);
        //    //}
        //}

        private SkeletonElement _selectedBone;
        public SkeletonElement BSkeletonComboxSelected // TODO: cleanup
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value);/* AnySkeletonSelected = true;*/ }
        }

        //private List<SkeletonElement> _shops;

        //protected void FilterShops()
        //{
        //    ComboOpen.Value = true;
        //    if (!string.IsNullOrEmpty(SearchText))
        //    {
        //        Shops.UpdateSource(_shops.Where(s => s.NameExtended.ToLower().Contains(SearchText.ToLower())));
        //    }
        //    else
        //    {
        //        Shops.UpdateSource(_shops);
        //    }
        //    OnPropertyChanged("Shops");
        //}



        public FBXSettingsViewModel(PackFileService packFileSericcee, FbxSettingsModel fbxSettings)
        {
            _packFileService = packFileSericcee;
            _fbxSettings = fbxSettings; // TODO: is this system you want to keep, it probably is?
            FillFileInfoPanel();
            UpdateViewData();
        }

        // TODO: MAYBE not needed anymore, when the DataGrid is not longer used
        private void FillFileInfoPanel()
        {

        }

        public void SkeletonFileBrowseButton()
        {
            var test = BSkeletonComboxSelected.Name;
            var DEBUG_BREAK = true; // TODO: REMOVE!!
        }

        public void ImportButtonClicked()
        {
            // not needed anymore...?            
        }
        public void BrowseButtonClicked()
        {
            // TODO: use (animation) SelectionListWindow.ShowDialog() instead
            var dialog = new OpenFileDialog
            {
                Filter = "ANIM Files (*.anim)|*.anim|All files (*.*)|*.*\\",   // Clean this up so its correct based on the assetManagementFactory data
                Multiselect = false,
                Title = "Select .ANIM Skeleton File"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // TODO:  what is going on here??
                //var diskFile = new SkeletonElement();
                //diskFile.SkeletonPackFile = new PackFile(dialog.FileName, new FileSystemSource(dialog.FileName));

                //BSkeketonComboBoxContent.Insert(0, diskFile);

                //BSkeletonComboxSelected = diskFile;
            }
        }

        /// <summary>
        /// moves data from storeage class into UI controls
        /// </summary>
        private void UpdateViewData(/*FbxSettingsModel inSettingsModel*/) // TODO: Param needed, better to store from constructor? Or should FileInfoData be input, FbxSettigs output, DECIDE!!
        {
            // // TODO: needed?
            //SkeletonFileName.Value = inSettingsModel.SkeletonFileName;
            //SkeletonName.Value = inSettingsModel.SkeletonName;
            //UseAutoRigging.Value = inSettingsModel.UseAutoRigging;



            const string skeletonFolder = @"animations\skeletons\";
            const string animExtension = ".anim";
            var searchResult = _packFileService.FindAllFilesInDirectory(skeletonFolder);

            if (searchResult == null)
            { throw new Exception("Anim file search, 0 results!"); };

            _actualSkeletonList.Add(SkeletonElement.GetNoSkeletonElement());

            foreach (var animFile in searchResult)
            {
                if (Path.GetExtension(animFile.Name) == animExtension)
                {
                    _actualSkeletonList.Add(new SkeletonElement() { SkeletonPackFile = animFile });
                }
            }

            BSkeletonComboxSelected = new SkeletonElement();

            if (_fbxSettings.FileInfoData.SkeletonName.Any())
                SetSkeletonFromName(_fbxSettings.FileInfoData.SkeletonName);

            BSkeketonComboBoxContent = _actualSkeletonList;
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private void GetViewData(FbxSettingsModel outSettingsModel)
        {
            outSettingsModel.SkeletonFileName = BSkeletonComboxSearchText;
            outSettingsModel.SkeletonName = SkeletonFileName.Value;
            outSettingsModel.UseAutoRigging = UseAutoRigging.Value;
            outSettingsModel.SkeletonFile = GetSkeletonFileFromView();
        }

        private void SetSkeletonFromName(string skeletonName)
        {
            foreach (var skeleton in _actualSkeletonList)
            {
                var fileSkeletonName = Path.GetFileNameWithoutExtension(skeleton.Name);
                if (fileSkeletonName.Equals(skeletonName, StringComparison.OrdinalIgnoreCase))
                {
                    BSkeletonComboxSelected = skeleton;
                    return; // found so exist
                }
            }

            // none found, set empty skeleton in combobox
            BSkeletonComboxSelected = new SkeletonElement();
        }

        private AnimationFile GetSkeletonFileFromView()
        {
            if (BSkeletonComboxSelected == null)
                return null;

            var skeletonFile = BSkeletonComboxSelected.SkeletonPackFile;
            if (skeletonFile == null)
                return null;

            var skeletonAnim = AnimationFile.Create(skeletonFile);

            return skeletonAnim;
        }

        /// <summary>
        /// Static helper, essentially taken from "PinToolViewModel"
        /// </summary>        
        static public bool ShowImportDialog(PackFileService packFileSericcee, FbxSettingsModel fbxImportSettingsModel)
        {
            var dialog = new FbxSettingsDialogView();
            var modelView = new FBXSettingsViewModel(packFileSericcee, fbxImportSettingsModel);
            dialog.DataContext = modelView;

            var result = dialog.ShowDialog().Value;
            modelView.GetViewData(fbxImportSettingsModel);

            return result;
        }
    }
}
