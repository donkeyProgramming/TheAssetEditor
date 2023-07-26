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

namespace AssetManagement.Strategies.Fbx.ViewModels
{

    public class FileInfoItem
    {
        public FileInfoItem() { }
        public FileInfoItem(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Elementsfor the Skeleeto Select ComboBox
    /// </summary>
    public class SkeletonElement : NotifyPropertyChangedImpl
    {
        public string Name
        {
            get
            {
                return SkeletonPackFile.Name;
            }

            set { }
        }

        public string Value { get; set; } = "Test Value";
        public PackFile SkeletonPackFile { get; set; }
    }

    public class FBXSettingsViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _packFileService;
        public ObservableCollection<FileInfoItem> FileInfoGridSource { get; set; } = new ObservableCollection<FileInfoItem>();
        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();        
        public NotifyAttr<string> BTextSkeletonCombox { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<bool> UseAutoRigging { get; set; } = new NotifyAttr<bool>();

        public ObservableCollection<SkeletonElement> BSkeketonComboBox { get; set; } = new ObservableCollection<SkeletonElement>();

        private SkeletonElement _selectedBone;    
        public SkeletonElement BSelectedSkeleton
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); }
        }

        public FBXSettingsViewModel(PackFileService packFileSericcee)
        {
            _packFileService = packFileSericcee;
            FillFileInfoPanel();
        }

        private void FillFileInfoPanel()
        {
            FileInfoGridSource.Add(new FileInfoItem("Skeleton Name (From Node)", "`humamnoid01`"));
            FileInfoGridSource.Add(new FileInfoItem("Units", "Inches"));
            FileInfoGridSource.Add(new FileInfoItem("Mesh Count", "3"));
            FileInfoGridSource.Add(new FileInfoItem("Materials Count", "3"));
            FileInfoGridSource.Add(new FileInfoItem("Animation", "1"));
            FileInfoGridSource.Add(new FileInfoItem("Animation Name ", "`puch_yourself_in_dick`"));
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
        private void SetViewData(FbxSettingsModel inSettingsModel)
        {
            SkeletonFileName.Value = inSettingsModel.SkeletonFileName;
            SkeletonName.Value = inSettingsModel.SkeletonName;
            UseAutoRigging.Value = inSettingsModel.UseAutoRigging;

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

            if (BSkeketonComboBox.Any())
                BSelectedSkeleton = BSkeketonComboBox[0];

            if (SkeletonName.Value.Any())
                SetSkeletonFromName(SkeletonName.Value);

            // TODO: get this WORK !!!
            BTextSkeletonCombox.Value = "--None Selected--";
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private FbxSettingsModel GetViewData(FbxSettingsModel outSettingsModel)        {          

            outSettingsModel.SkeletonFileName = SkeletonFileName.Value;
            outSettingsModel.SkeletonName = SkeletonFileName.Value;
            outSettingsModel.UseAutoRigging = UseAutoRigging.Value;
            outSettingsModel.SkeletonFile = GetSkeletonFileFromView();

            return outSettingsModel;
        }


        private void SetSkeletonFromName(string skeletonName)
        {
            foreach (var skeleton in BSkeketonComboBox)
            {
                if (Path.GetFileNameWithoutExtension(skeleton.Name).Equals(skeletonName, StringComparison.OrdinalIgnoreCase))
                {
                    BSelectedSkeleton = skeleton;
                }
            }
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
        static public bool ShowImportDialog(PackFileService packFileSericcee,  FbxSettingsModel fbxImportSettingsModel)
        {
            var dialog = new FBXSetttingsView();
            var modelView = new FBXSettingsViewModel(packFileSericcee);

            dialog.DataContext = modelView;
            
            modelView.SetViewData(fbxImportSettingsModel);

            var result = dialog.ShowDialog().Value;
            modelView.GetViewData(fbxImportSettingsModel);

            return result;
        }
    }
}
