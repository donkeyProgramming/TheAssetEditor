using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews
{
    public partial class DefaultViewModel : ObservableObject
    {
        private readonly DefaultCapability _defaultCapability;

        [ObservableProperty] bool _useAlpha;
        [ObservableProperty] ShaderTextureViewModel _baseColour;
        [ObservableProperty] ShaderTextureViewModel _materialMap;
        [ObservableProperty] ShaderTextureViewModel _normalMap;
        [ObservableProperty] ShaderTextureViewModel _mask;

        public DefaultViewModel(Rmv2MeshNode typedNode, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            var defaultCapability = typedNode.Effect.GetCapability<DefaultCapability>();
            Guard.IsNotNull(defaultCapability);
            _defaultCapability = defaultCapability;

            _baseColour = new ShaderTextureViewModel(TextureType.BaseColour, defaultCapability, packFileService, uiCommandFactory, resourceLibrary);
            _materialMap = new ShaderTextureViewModel(TextureType.MaterialMap, defaultCapability, packFileService, uiCommandFactory, resourceLibrary);
            _normalMap = new ShaderTextureViewModel(TextureType.Normal, defaultCapability, packFileService, uiCommandFactory, resourceLibrary);
            _mask = new ShaderTextureViewModel(TextureType.Mask, defaultCapability, packFileService, uiCommandFactory, resourceLibrary);

            _useAlpha = defaultCapability.UseAlpha;
        }

        partial void OnUseAlphaChanged(bool value) => _defaultCapability.UseAlpha = value;
    }
}
