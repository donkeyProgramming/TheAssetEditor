using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using System.Collections.Generic;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectMeshViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _data;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;

        public SelectMeshViewModel(PackFileService pfs, AssetViewModel data, AssetViewModelBuilder assetViewModelBuilder)
        {
            _pfs = pfs;
            _data = data;
            _assetViewModelBuilder = assetViewModelBuilder;
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
            _assetViewModelBuilder.SetMesh(_data, meshPackFile);
        }
    }


}
