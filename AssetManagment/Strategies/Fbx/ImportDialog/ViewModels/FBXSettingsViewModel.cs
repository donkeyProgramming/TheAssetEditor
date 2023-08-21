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
        private readonly FbxSettingsModel _inputFbxSettings; // Maybe "FileInfoDat" should store only, and FBXSettings is for "get info from dialog", not something that already contains data, or not?        

        public FBXSettingsViewModel(PackFileService packFileSericcee, FbxSettingsModel fbxSettings)
        {
            _packFileService = packFileSericcee;
            _inputFbxSettings = fbxSettings; // TODO: is this system you want to keep, it probably is?
            FillFileInfoPanel();
            UpdateViewData();
        }

        // -- Asset File Info Panel (maybe should have its own ViewModel?)
        public string FileName { get { return _inputFbxSettings.FileInfoData.FileName; } set { _inputFbxSettings.FileInfoData.FileName = value; NotifyPropertyChanged(); } }
        public string SdkVersion
        {
            get { return $"{_inputFbxSettings.FileInfoData.SdkVersionUsed.X}.{_inputFbxSettings.FileInfoData.SdkVersionUsed.Y}.{_inputFbxSettings.FileInfoData.SdkVersionUsed.Z}"; }
            set {; }
        }
        public string SkeletonNodeName { get { return _inputFbxSettings.FileInfoData.SkeletonName; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string Units { get { return _inputFbxSettings.FileInfoData.Units; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MeshCount { get { return $"{_inputFbxSettings.FileInfoData.MeshCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string NodeCount { get { return $"{_inputFbxSettings.FileInfoData.ElementCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MaterialCount { get { return $"{_inputFbxSettings.FileInfoData.MaterialCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string AnimationCount { get { return $"Num{_inputFbxSettings.FileInfoData.AnimationsCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string BoneCount { get { return $"Num{_inputFbxSettings.FileInfoData.BoneCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string DerformationData { get { return "Correct: 2221 Vertex Influences Found"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }

        // -- Asset Animations Info  (maybe should have its own ViewModel?)
        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();

        // TODO: change to "use/apply rigging", as there is no "auto-riggin"
        public NotifyAttr<bool> ApplyRigging { get; set; } = new NotifyAttr<bool>(true);
        
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
        
        private SkeletonElement _selectedBone;
        public SkeletonElement BSkeletonComboxSelected // TODO: cleanup
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value);/* AnySkeletonSelected = true;*/ }
        }

        
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

            const string skeletonFolder = @"animations\skeletons\";
            const string animExtension = ".anim";
            var searchResult = _packFileService.FindAllFilesInDirectory(skeletonFolder);

            if (searchResult == null)
            { throw new Exception("Anim file search, 0 results!"); };

            BSkeketonComboBoxContent.Add(SkeletonElement.GetNoSkeletonElement());

            foreach (var animFile in searchResult)
            {
                if (Path.GetExtension(animFile.Name) == animExtension)
                {
                    BSkeketonComboBoxContent.Add(new SkeletonElement() { SkeletonPackFile = animFile });
                }
            }

            BSkeletonComboxSelected = new SkeletonElement();

            if (_inputFbxSettings.FileInfoData.SkeletonName.Any())
                SetSkeletonFromName(_inputFbxSettings.FileInfoData.SkeletonName);
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private void GetViewData(FbxSettingsModel outSettingsModel)
        {
            outSettingsModel.SkeletonFileName = (BSkeletonComboxSelected.SkeletonPackFile == null) ? "" : BSkeletonComboxSelected.Name;
            outSettingsModel.SkeletonName = (BSkeletonComboxSelected.SkeletonPackFile == null) ? "" : _inputFbxSettings.SkeletonName;
            outSettingsModel.ApplyRigging = ApplyRigging.Value;
            outSettingsModel.SkeletonFile = GetSkeletonFileFromView();
        }
        
        /// <summary>
        /// Tries to select the skeleton in the combox that matches the string
        /// if none found, select "Empty Skeleton" = ("no skeleton")
        /// </summary>
        /// <param name="skeletonName">search string</param>
        private void SetSkeletonFromName(string skeletonName)
        {
            foreach (var skeleton in BSkeketonComboBoxContent)
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
            if (result)
                modelView.GetViewData(fbxImportSettingsModel);

            return result;
        }
    }
}
