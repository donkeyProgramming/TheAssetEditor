using System.Collections.Generic;
using CommonControls.PackFileBrowser;
using SharedCore.Misc;
using SharedCore.PackFiles;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectMeshViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _pfs;
        private readonly SceneObject _data;
        private readonly SceneObjectBuilder _assetViewModelBuilder;

        public SelectMeshViewModel(PackFileService pfs, SceneObject data, SceneObjectBuilder assetViewModelBuilder)
        {
            _pfs = pfs;
            _data = data;
            _assetViewModelBuilder = assetViewModelBuilder;
        }

        public void BrowseMesh()
        {
            using var browser = new PackFileBrowserWindow(_pfs);
            browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                var file = browser.SelectedFile;
                _assetViewModelBuilder.SetMesh(_data, file);
            }
        }
    }
}
