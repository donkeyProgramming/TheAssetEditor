using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;
using GameWorld.Core.Utility.UserInterface;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.DirtAndDecal
{
    public partial class DirtAndDecalViewModel : ObservableObject
    {
        private readonly DirtAndDecalCapability _capability;

        [ObservableProperty] ShaderTextureViewModel _dirtMap;
        [ObservableProperty] ShaderTextureViewModel _dirtMask;
        [ObservableProperty] ShaderTextureViewModel _decalMask;

        [ObservableProperty] Vector2ViewModel _uvScale;
        [ObservableProperty] Vector4ViewModel _textureTransform;

        [ObservableProperty] ShaderTextureViewModel _decalPreviewColour;
        [ObservableProperty] ShaderTextureViewModel _decalPreviewNormal;

        [ObservableProperty] bool _useDecal;

        public DirtAndDecalViewModel(DirtAndDecalCapability capability, IUiCommandFactory uiCommandFactory, IPackFileService packFileService, ResourceLibrary resourceLibrary, IPackFileUiProvider packFileUiProvider)
        {
            _capability = capability;

            _dirtMap = new ShaderTextureViewModel(_capability.DirtMap, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _dirtMask = new ShaderTextureViewModel(_capability.DirtMask, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _decalMask = new ShaderTextureViewModel(_capability.DecalMask, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);

            _uvScale = new Vector2ViewModel(_capability.UvScale, OnUvScaleChanged);
            _textureTransform = new Vector4ViewModel(_capability.TextureTransform, OnTextureTransformChanged);

            _decalPreviewColour = new ShaderTextureViewModel(_capability.DecalPreviewColour, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _decalPreviewNormal = new ShaderTextureViewModel(_capability.DecalPreviewNormal, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
        }

        void OnUvScaleChanged(Vector2 value) => _capability.UvScale = value;
        void OnTextureTransformChanged(Vector4 value) => _capability.TextureTransform = value;
        partial void OnUseDecalChanged(bool value) => _capability.UseDecal = value;
    }
}
