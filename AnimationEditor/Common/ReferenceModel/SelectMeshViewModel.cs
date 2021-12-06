using Common;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectMeshViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _data;

        public SelectMeshViewModel(PackFileService pfs, AssetViewModel data)
        {
            _pfs = pfs;
            _data = data;
        }

        public void BrowseMesh()
        {
            using (var browser = new PackFileBrowserWindow(_pfs))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
                if (browser.ShowDialog() == true && browser.SelectedFile != null)
                {
                    var file = browser.SelectedFile;
                    SetMesh(file);
                }
            }
        }

        public void SetMesh(PackFile meshPackFile)
        {
            _data.SetMesh(meshPackFile);
        }
    }


}
