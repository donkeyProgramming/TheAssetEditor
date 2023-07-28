using CommonControls.Common;
using AssetManagement.Strategies.Fbx.Models;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using AssetManagement.Strategies.Fbx.Views.FBXImportSettingsDialogView;
using CommonControls.Services;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using CommonControls.FileTypes.Animation;
using System.Drawing;
using AssetManagement.GenericFormats.Managed;
using AssetManagement.GenericFormats.Unmanaged;
// TODO: clean "using"s (EVERYWHERE, not just here)


// finish "rationaliztion" this ModelView, around the concep of "FileInfoData" as input
namespace AssetManagement.Strategies.Fbx.ViewModels
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
                return (SkeletonPackFile != null) ? SkeletonPackFile.Name : "";
            }

            set { }
        }

        public string Value { get; set; } = "Test Value";
        public PackFile SkeletonPackFile { get; set; }
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
            set {/* _fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } // TODO is "set" needed when it is ONLY for one way info
        }

        public string SkeletonName { get { return _fbxSettings.FileInfoData.SkeletonName; } set { _fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged(); } }


        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<bool> UseAutoRigging { get; set; } = new NotifyAttr<bool>();
        public ObservableCollection<SkeletonElement> BSkeketonComboBox { get; set; } = new ObservableCollection<SkeletonElement>();

        // TODO: remove?
        //public bool AnySkeletonSelected { get; set; } = false;   

        public NotifyAttr<string> BTextSkeletonCombox { get; set; } = new NotifyAttr<string>();

        private SkeletonElement _selectedBone;
        public SkeletonElement BSelectedSkeleton // TODO: cleanup
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value);/* AnySkeletonSelected = true;*/ }
        }

        public FBXSettingsViewModel(PackFileService packFileSericcee, FbxSettingsModel fbxSettings)
        {
            _packFileService = packFileSericcee;
            _fbxSettings = fbxSettings; // TODO: is this system you want to keep, it probably is?
            FillFileInfoPanel();
        }

        // TODO: MAYBE not needed anymore, when the DataGrid is not longer used
        private void FillFileInfoPanel()
        {

        }

        public void SkeletonFileBrowseButton()
        {
            var test = BSelectedSkeleton.Name;
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
                SkeletonFileName.Value = dialog.FileName;
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
                throw new Exception("Anim file search, 0 results!");

            foreach (var animFile in searchResult)
            {
                if (Path.GetExtension(animFile.Name) == animExtension)
                {
                    BSkeketonComboBox.Add(new SkeletonElement() { SkeletonPackFile = animFile });
                }
            }

            BSelectedSkeleton = new SkeletonElement();

            if (_fbxSettings.FileInfoData.SkeletonName.Any())
                SetSkeletonFromName(_fbxSettings.FileInfoData.SkeletonName);                        
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private void GetViewData(FbxSettingsModel outSettingsModel)
        {
            outSettingsModel.SkeletonFileName = SkeletonFileName.Value;
            outSettingsModel.SkeletonName = SkeletonFileName.Value;
            outSettingsModel.UseAutoRigging = UseAutoRigging.Value;
            outSettingsModel.SkeletonFile = GetSkeletonFileFromView();            
        }

        private void SetSkeletonFromName(string skeletonName)
        {
            foreach (var skeleton in BSkeketonComboBox)
            {
                if (Path.GetFileNameWithoutExtension(skeleton.Name).Equals(skeletonName, StringComparison.OrdinalIgnoreCase))
                {
                    BSelectedSkeleton = skeleton;
                    return; // found so exist
                }
            }

            // none found, set empty skeleton in combobox
            BSelectedSkeleton = new SkeletonElement();
        }

        private AnimationFile GetSkeletonFileFromView()
        {
            if (BSelectedSkeleton == null)
                return null;

            var skeletonFile = BSelectedSkeleton.SkeletonPackFile;
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
            var dialog = new FBXSetttingsView();
            var modelView = new FBXSettingsViewModel(packFileSericcee, fbxImportSettingsModel);

            dialog.DataContext = modelView;

            // TODO: needed?
            modelView.UpdateViewData();

            var result = dialog.ShowDialog().Value;
            modelView.GetViewData(fbxImportSettingsModel);

            return result;
        }
    }
}
