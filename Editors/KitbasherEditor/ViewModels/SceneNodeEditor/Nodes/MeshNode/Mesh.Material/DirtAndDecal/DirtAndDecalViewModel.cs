using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.DirtAndDecal
{
    public partial class DirtAndDecalViewModel : ObservableObject
    {
        private readonly DirtAndDecalCapability _capability;


        public DirtAndDecalViewModel(DirtAndDecalCapability capability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _capability = capability;

           //_bloodMap = new ShaderTextureViewModel(bloodCapability.BloodMask, packFileService, uiCommandFactory, resourceLibrary);
           //_useBlood = _bloodCapability.UseBlood;
           //_bloodUvScale = new Vector2ViewModel(_bloodCapability.UvScale, OnBloodUvScaleChanged);
           //_bloodPreview = new FloatViewModel(_bloodCapability.PreviewBlood, OnBloodPreviewChanged);
        }

      // void OnBloodUvScaleChanged(Vector2 value) => _bloodCapability.UvScale = value;
      // partial void OnUseBloodChanged(bool value) => _bloodCapability.UseBlood = value;
      // void OnBloodPreviewChanged(float value) => _bloodCapability.PreviewBlood = value;
    }
}
