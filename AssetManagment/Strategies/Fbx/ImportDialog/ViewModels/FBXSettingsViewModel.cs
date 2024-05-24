using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using AssetManagement.Strategies.Fbx.ImportDialog.DataModels;
using AssetManagement.Strategies.Fbx.ImportDialog.Views;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;

// finish "rationaliztion" this ModelView, around the concept of "FileInfoData" as input
namespace AssetManagement.Strategies.Fbx.ImportDialog.ViewModels
{
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

            set
            {                
                Name = value;
            }
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
            _inputFbxSettings = fbxSettings; 
            FillFileInfoPanel();
            UpdateViewData();
        }

        // -- Asset File Info Panel (maybe should have its own ViewModel?)
        public string FileName
        {
            get { return _inputFbxSettings.FileInfoData.FileName; }
            set
            {
                _inputFbxSettings.FileInfoData.FileName = value;
                NotifyPropertyChanged(nameof(_inputFbxSettings.FileInfoData.FileName));
            }
        }

        public string SdkVersion
        {
            get { return $"{_inputFbxSettings.FileInfoData.SdkVersionUsed.X}.{_inputFbxSettings.FileInfoData.SdkVersionUsed.Y}.{_inputFbxSettings.FileInfoData.SdkVersionUsed.Z}"; }
            set {; }
        }
        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();
        public string SkeletonNodeName { get { return _inputFbxSettings.FileInfoData.SkeletonName; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string Units { get { return _inputFbxSettings.FileInfoData.Units; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MeshCount { get { return $"{_inputFbxSettings.FileInfoData.MeshCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string NodeCount { get { return $"{_inputFbxSettings.FileInfoData.ElementCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MaterialCount { get { return $"{_inputFbxSettings.FileInfoData.MaterialCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string AnimationCount { get { return $"{_inputFbxSettings.FileInfoData.AnimationsCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string BoneCount
        {
            get
            {   
                // In the FBX FILE "SkeletonName" is encoded in an "fbxsdk::FbxSkeletonNode", as it is the only data that is reliably re-saved from Blender, 
                // so, if the FbxSettings contains a valid skeleton name, it means there is an extra bone, 
                // which  is not part of the "TW ANIM" Skeleton hierachy, in that case subtract that.
                int actualBoneCount = 
                    _inputFbxSettings.FileInfoData.BoneCount - 
                    (_inputFbxSettings.SkeletonName != null && _inputFbxSettings.SkeletonName.Any() ? 1 : 0);
                
                 return $"{actualBoneCount}";
            }
            set
            {               
                _inputFbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged();                
            }
        }

        public string DerformationData
        {
            get
            {
                return (_inputFbxSettings.FileInfoData.ContainsDerformingData) ? "Yes" : "No";
            }
            
            set { _inputFbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged(); }
        }


        // -- Asset Animations Info  (maybe should have its own ViewModel?)

        /// <summary>
        /// Hides the animation/deformation options if there is no and/or invalid derformation/animation data 
        /// </summary>
        public Visibility AnimationPanelVisibility
        {
            get { return _inputFbxSettings.FileInfoData.ContainsDerformingData ? Visibility.Visible : Visibility.Hidden; }
        }       

        public NotifyAttr<bool> BSkeletonApplyRigging { get; set; } = new NotifyAttr<bool>(true);

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

        private SkeletonElement _selectedSkeleton;
        
        public SkeletonElement BSkeletonComboxSelected 
        { 
            get { return _selectedSkeleton; }
            set { SetAndNotify(ref _selectedSkeleton, value);/*feature swichted off  AnySkeletonSelected = true;*/ }
        }

        private void FillFileInfoPanel()
        {

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
                // TODO:  what is going on here?? Check that is actually works
                var diskFile = new SkeletonElement();
                diskFile.SkeletonPackFile = new PackFile(dialog.FileName, new FileSystemSource(dialog.FileName));
                BSkeketonComboBoxContent.Insert(0, diskFile);
                BSkeletonComboxSelected = diskFile;
            }
        }

        /// <summary>
        /// moves data from storeage class into UI controls
        /// </summary>
        private void UpdateViewData() 
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
            outSettingsModel.ApplyRiggingData = BSkeletonApplyRigging.Value;

            if (BSkeletonComboxSelected.SkeletonPackFile != null && outSettingsModel.ApplyRiggingData)
            {
                outSettingsModel.SkeletonFileName = BSkeletonComboxSelected.Name;
                outSettingsModel.SkeletonName = _inputFbxSettings.SkeletonName;
                outSettingsModel.SkeletonPackFile = GetSkeletonPackFileFromView();
            }
            else
            {
                outSettingsModel.SkeletonFileName = "";
                outSettingsModel.SkeletonName = "";
                outSettingsModel.SkeletonPackFile = null;
            }
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

        private AnimationFile GetSkeletonPackFileFromView()
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
