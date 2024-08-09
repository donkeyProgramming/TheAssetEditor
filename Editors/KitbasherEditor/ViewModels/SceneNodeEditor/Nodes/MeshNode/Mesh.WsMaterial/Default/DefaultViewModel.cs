using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews
{
    public partial class DefaultViewModel : ObservableObject
    {
        private readonly DefaultCapabilityMetalRough _defaultCapability;

        [ObservableProperty] bool _useAlpha;
        
        [ObservableProperty] ShaderTextureViewModel _baseColour;
        [ObservableProperty] ShaderTextureViewModel _materialMap;
        [ObservableProperty] ShaderTextureViewModel _normalMap;
        [ObservableProperty] ShaderTextureViewModel _mask;

        public DefaultViewModel(DefaultCapabilityMetalRough defaultCapability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _defaultCapability = defaultCapability;

            _baseColour = new ShaderTextureViewModel(defaultCapability.BaseColour, packFileService, uiCommandFactory, resourceLibrary);
            _materialMap = new ShaderTextureViewModel(defaultCapability.MaterialMap, packFileService, uiCommandFactory, resourceLibrary);
            _normalMap = new ShaderTextureViewModel(defaultCapability.NormalMap, packFileService, uiCommandFactory, resourceLibrary);
            _mask = new ShaderTextureViewModel(defaultCapability.Mask, packFileService, uiCommandFactory, resourceLibrary);

            _useAlpha = defaultCapability.UseAlpha;
        }

        partial void OnUseAlphaChanged(bool value) => _defaultCapability.UseAlpha = value;
    }
}
