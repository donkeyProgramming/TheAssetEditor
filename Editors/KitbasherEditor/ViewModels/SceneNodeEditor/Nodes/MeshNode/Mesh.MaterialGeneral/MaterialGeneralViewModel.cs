using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.SceneNodes;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public partial class MaterialGeneralViewModel : ObservableObject
    {

        private readonly PackFileService _pfs;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IUiCommandFactory _uiCommandFactory;

        Rmv2MeshNode _meshNode;

        [ObservableProperty] ObservableCollection<TextureViewModel> _textureList = [];
        [ObservableProperty] string _textureDirectory;
        [ObservableProperty] bool _useAlpha;
        [ObservableProperty] bool _onlyShowUsedTextures = true;

        public MaterialGeneralViewModel(PackFileService pfs, ApplicationSettingsService applicationSettings, IUiCommandFactory uiCommandFactory)
        {
            _pfs = pfs;
            _uiCommandFactory = uiCommandFactory;
            _applicationSettingsService = applicationSettings;
        }

        public void Initialize(Rmv2MeshNode meshNode)
        {
            _meshNode = meshNode;

            UseAlpha = _meshNode.Material.AlphaMode == AlphaMode.Transparent;
            TextureDirectory = _meshNode.Material.TextureDirectory;
            RefreshTextureList();
        }

        void RefreshTextureList()
        {
            var enumValues = Enum.GetValues(typeof(TextureType)).Cast<TextureType>().ToList();
            var textureEnumValues = _meshNode.Material.GetAllTextures().Select(x => x.TexureType).ToList();
            enumValues.AddRange(textureEnumValues);
            var distinctEnumList = enumValues.Distinct();

            TextureList.Clear();
            foreach (var enumValue in distinctEnumList)
            {
                var textureView = new TextureViewModel(_meshNode, enumValue, _pfs, _uiCommandFactory);
                TextureList.Add(textureView);
            }

            OnOnlyShowUsedTexturesChanged(OnlyShowUsedTextures);
        }

        partial void OnUseAlphaChanged(bool value)
        {
            if (value)
                _meshNode.Material.AlphaMode = AlphaMode.Transparent;
            else
                _meshNode.Material.AlphaMode = AlphaMode.Opaque;
        }


        partial void OnTextureDirectoryChanged(string value)
        {
            _meshNode.Material.TextureDirectory = value;
        }

        partial void OnOnlyShowUsedTexturesChanged(bool value)
        {
            foreach (var texture in TextureList)
            {
                if (_applicationSettingsService.CurrentSettings.HideWh2TextureSelectors &&
                   _applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3)
                {
                    if (texture.TexureType == TextureType.Diffuse ||
                       texture.TexureType == TextureType.Specular ||
                       texture.TexureType == TextureType.Gloss)
                    {
                        texture.IsVisible.Value = false;
                        continue;
                    }
                }
                if (value == false)
                    texture.IsVisible.Value = true;
                else
                    texture.IsVisible.Value = !string.IsNullOrWhiteSpace(texture.Path);
            }
        }

        [RelayCommand]
        void ResolveMissingTextures() => _uiCommandFactory.Create<DeleteMissingTexturesCommand>().Execute(_meshNode);

        [RelayCommand]
        void DeleteMissingTextures()
        {
            _uiCommandFactory.Create<DeleteMissingTexturesCommand>().Execute(_meshNode);
            RefreshTextureList();
        }

    }
}
