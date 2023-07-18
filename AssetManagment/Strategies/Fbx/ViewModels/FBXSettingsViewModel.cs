using AssetManagement.GenericFormats.Unmanaged;
using AssetManagement.Strategies.Fbx.Views.FBXSettings;
using CommonControls.Common;
using AssetManagement.Strategies.Fbx.Models;
using System.Windows.Forms;
using System.ComponentModel;

namespace AssetManagement.Strategies.Fbx.ViewModels
{
    public class FBXSettingsViewModel : NotifyPropertyChangedImpl
    {
        public FbxSettingsModel ImportSettings { get; set; }

        public FBXSettingsViewModel(FbxSettingsModel fbxImportSettingsModel)
        {
            ImportSettings = fbxImportSettingsModel;
            SetViewData();
        }

        public void ImportButtonClicked() => OnImportButtonClicked();

        public void OnImportButtonClicked()
        {
            GetViewData();
        }


        public void BrowseButtonClicked() => OnBrowseButtonClicked();
        public void OnBrowseButtonClicked()
        {
            // TODO: use SelectionListWindow.ShowDialog() instead
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

        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<bool> AutoRigCheckBox { get; set; } = new NotifyAttr<bool>();


        //private bool? _autoRigCheckBox = false;
        //public bool AutoRigCheckBox
        //{
        //    get { return (_autoRigCheckBox != null) ? _autoRigCheckBox.Value : false; } 

        //    set { _autoRigCheckBox = value; }
        //}

        /// <summary>
        /// moves data from storeage class into UI controls
        /// </summary>
        private void SetViewData()
        {
            SkeletonFileName.Value = ImportSettings.SkeletonFileName;
            SkeletonName.Value = ImportSettings.SkeletonName;

            if (ImportSettings.SkeletonName != "")
            {
                AutoRigCheckBox.Value = true;
            }
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private void GetViewData()
        {
            ImportSettings.SkeletonFileName = SkeletonFileName.Value;
            ImportSettings.SkeletonName = SkeletonFileName.Value;
            ImportSettings.UseAutoRigging = AutoRigCheckBox.Value;
        }

        /// <summary>
        /// Static helper, essentially taken from "PinToolViewModel"
        /// </summary>        
        static public bool ShowImportDialog(FbxSettingsModel fbxImportSettingsModel)
        {
            var dialog = new FBXSetttingsView();
            var modelView = new FBXSettingsViewModel(fbxImportSettingsModel);

            dialog.DataContext = modelView;
            modelView.SetViewData();

            var result = dialog.ShowDialog().Value;
            modelView.GetViewData();

            return result;
        }
    }
}
