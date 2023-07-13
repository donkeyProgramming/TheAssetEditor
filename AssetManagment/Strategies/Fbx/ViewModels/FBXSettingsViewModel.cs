using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;
using AssetManagement.Strategies.Fbx.Views.FBXSettings;
using CommonControls.BaseDialogs;
using CommonControls.Common;

using CommonControls.Editors.ModelImportSettings;

namespace AssetManagement.Strategies.Fbx.ViewModels
{
    public class FBXSettingsViewModel
    {
        public FBXImportExportSettings ImportSettings { get; set; }

        public FBXSettingsViewModel(FBXImportExportSettings fbxIportSettings)
        {
            ImportSettings = fbxIportSettings;
        }

        public void ImportButtonClicked() => OnImportButtonClicked();

        public void OnImportButtonClicked()
        {
            FetchControlData();
        }

        public NotifyAttr<string> FileNameTextBox { get; set; } = new NotifyAttr<string>($"empty.fbx");
        public NotifyAttr<string> UnitNameTextBox { get; set; } = new NotifyAttr<string>($"Inches");
        public NotifyAttr<string> CoordSystemTextBox { get; set; } = new NotifyAttr<string>($"Y-Up");

        /// <summary>
        /// moves data from storeage class into UI controls
        /// </summary>
        private void UpdataControlData()
        {
            FileNameTextBox.Value = ImportSettings.fileName;
        }

        /// <summary>
        /// moves data from UI control into storeage class
        /// </summary>
        private void FetchControlData()
        {
            ImportSettings.fileName = FileNameTextBox.Value;
        }

        /// <summary>
        /// Static helper, essentially taken from "PinToolViewModel"
        /// </summary>        
        static public bool ShowImportDialog(FBXImportExportSettings fbxIportSettings)
        {
            var dialog = new FBXSetttingsView();
            var model = new FBXSettingsViewModel(fbxIportSettings);

            dialog.DataContext = model;
            model.UpdataControlData();

            var result = dialog.ShowDialog().Value;
            model.FetchControlData();

            return result;
        }
    }
}
