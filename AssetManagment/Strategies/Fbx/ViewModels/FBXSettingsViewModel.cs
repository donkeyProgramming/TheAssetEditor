using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using AssetManagement.Strategies.Fbx.Views.FBXSettings;
using CommonControls.BaseDialogs;
using CommonControls.Common;

namespace AssetManagement.Strategies.Fbx.ViewModels
{
    

    public class FBXSettingsViewModel
    {
        public FBXImportSettings ImportSettings { get; set; }


        public FBXSettingsViewModel(FBXImportSettings fbxIportSettings)
        {
            ImportSettings = fbxIportSettings;
        }

        public void ImportButtonClicked() => DoStuff();

        public NotifyAttr<string> SelectedForStaticDescription { get; set; } = new NotifyAttr<string>($"You have not yet clicked!");

        public void DoStuff()
        {
            SelectedForStaticDescription.Value = "Now You Have Clicked 'IMPORT'";
             var DEBUG_BREAK = 1;
        }


        public void ShowWindow()
        {
            var window = new ControllerHostWindow(true)
            {
                DataContext = this,
                Title = "Stuff",
                Content = new FBXSetttingsView(),
                Width = 360,
                Height = 415,
            };
            window.ShowDialog();
        }

    }
}
