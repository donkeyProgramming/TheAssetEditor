using AssetManagement.Strategies.Fbx.Views.FBXSettings;
using AssetManagement.Strategies.Fbx.Models;
using System.Windows.Forms;
using Shared.Core.Misc;

namespace AssetManagement.Strategies.Fbx.ViewModels
{
    public class FBXSettingsViewModel : NotifyPropertyChangedImpl
    {        
        public NotifyAttr<string> SkeletonName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<bool> UseAutoRigging { get; set; } = new NotifyAttr<bool>();      
        
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
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private FbxSettingsModel GetViewData()
        {
            FbxSettingsModel outSettingsModel = new FbxSettingsModel();

            outSettingsModel.SkeletonFileName = SkeletonFileName.Value;
            outSettingsModel.SkeletonName = SkeletonFileName.Value;
            outSettingsModel.UseAutoRigging = UseAutoRigging.Value;

            return outSettingsModel;
        }

        /// <summary>
        /// Static helper, essentially taken from "PinToolViewModel"
        /// </summary>        
        static public bool ShowImportDialog(FbxSettingsModel fbxImportSettingsModel)
        {
            var dialog = new FBXSetttingsView();
            var modelView = new FBXSettingsViewModel();

            dialog.DataContext = modelView;
            modelView.SetViewData(fbxImportSettingsModel);

            var result = dialog.ShowDialog().Value;
            fbxImportSettingsModel = modelView.GetViewData();

            return result;
        }
    }
}
