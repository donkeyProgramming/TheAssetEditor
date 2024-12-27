using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;
using GameWorld.Core.Utility.UserInterface;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;

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

        public SpecGlossViewModel(SpecGlossCapability capability, IUiCommandFactory uiCommandFactory, IPackFileService packFileService, IScopedResourceLibrary resourceLibrary, IStandardDialogs packFileUiProvider)
        {
            _capability = capability;

            _specularMap = new ShaderTextureViewModel(capability.SpecularMap, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _glossMap = new ShaderTextureViewModel(capability.GlossMap, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _diffuseMap = new ShaderTextureViewModel(capability.DiffuseMap, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _normalMap = new ShaderTextureViewModel(capability.NormalMap, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _mask = new ShaderTextureViewModel(capability.Mask, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);

            _useAlpha = _capability.UseAlpha;
        }

        partial void OnUseAlphaChanged(bool value) => _capability.UseAlpha = value;
    }
}
