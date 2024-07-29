using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews
{
    public partial class DefaultViewModel : ObservableObject
    {
        private readonly DefaultCapability _defaultCapability;

        [ObservableProperty] bool _useAlpha;
        [ObservableProperty] bool _useFactionColours;
        
        [ObservableProperty] ShaderTextureViewModel _baseColour;
        [ObservableProperty] ShaderTextureViewModel _materialMap;
        [ObservableProperty] ShaderTextureViewModel _normalMap;
        [ObservableProperty] ShaderTextureViewModel _mask;

        public DefaultViewModel(DefaultCapability defaultCapability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _defaultCapability = defaultCapability;

            _baseColour = new ShaderTextureViewModel(defaultCapability.BaseColour, packFileService, uiCommandFactory, resourceLibrary);
            _materialMap = new ShaderTextureViewModel(defaultCapability.MaterialMap, packFileService, uiCommandFactory, resourceLibrary);
            _normalMap = new ShaderTextureViewModel(defaultCapability.NormalMap, packFileService, uiCommandFactory, resourceLibrary);
            _mask = new ShaderTextureViewModel(defaultCapability.Mask, packFileService, uiCommandFactory, resourceLibrary);

            _useAlpha = defaultCapability.UseAlpha;
            _useFactionColours = defaultCapability.UseFactionColours;
        }

        partial void OnUseAlphaChanged(bool value) => _defaultCapability.UseAlpha = value;
        partial void OnUseFactionColoursChanged(bool value) => _defaultCapability.UseFactionColours = value;
    }
}
