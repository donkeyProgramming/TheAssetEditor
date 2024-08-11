using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.SpecGloss
{
    public partial class SpecGlossViewModel : ObservableObject
    {
        private readonly SpecGlossCapability _capability;

        [ObservableProperty] bool _useAlpha;

        [ObservableProperty] ShaderTextureViewModel _specularMap;
        [ObservableProperty] ShaderTextureViewModel _glossMap;
        [ObservableProperty] ShaderTextureViewModel _diffuseMap;
        [ObservableProperty] ShaderTextureViewModel _normalMap;
        [ObservableProperty] ShaderTextureViewModel _mask;

        public SpecGlossViewModel(SpecGlossCapability capability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _capability = capability;

            _specularMap = new ShaderTextureViewModel(capability.SpecularMap, packFileService, uiCommandFactory, resourceLibrary);
            _glossMap = new ShaderTextureViewModel(capability.GlossMap, packFileService, uiCommandFactory, resourceLibrary);
            _diffuseMap = new ShaderTextureViewModel(capability.DiffuseMap, packFileService, uiCommandFactory, resourceLibrary);
            _normalMap = new ShaderTextureViewModel(capability.NormalMap, packFileService, uiCommandFactory, resourceLibrary);
            _mask = new ShaderTextureViewModel(capability.Mask, packFileService, uiCommandFactory, resourceLibrary);

            _useAlpha = _capability.UseAlpha;
        }

        partial void OnUseAlphaChanged(bool value) => _capability.UseAlpha = value;
    }
}
