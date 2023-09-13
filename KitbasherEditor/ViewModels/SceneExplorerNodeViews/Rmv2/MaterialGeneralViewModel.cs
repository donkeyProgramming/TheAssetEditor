using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public partial class MaterialGeneralViewModel : NotifyPropertyChangedImpl
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        Rmv2MeshNode _meshNode;
        PackFileService _pfs;
        ApplicationSettingsService _applicationSettingsService;

        public ICommand ResolveTexturesCommand { get; set; }
        public ICommand DeleteMissingTexturesCommand { get; set; }


        public bool UseAlpha
        {
            get => _meshNode.Material.AlphaMode == AlphaMode.Transparent;
            set
            {
                if (value)
                    _meshNode.Material.AlphaMode = AlphaMode.Transparent;
                else
                    _meshNode.Material.AlphaMode = AlphaMode.Opaque;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<TextureViewModel> TextureList { get; set; } = new ObservableCollection<TextureViewModel>();

        public string TextureDirectory
        {
            get => _meshNode.Material.TextureDirectory;
            set { _meshNode.Material.TextureDirectory = value; NotifyPropertyChanged(); }
        }

        bool _onlyShowUsedTextures = true;
        public bool OnlyShowUsedTextures
        {
            get => _onlyShowUsedTextures;
            set { SetAndNotify(ref _onlyShowUsedTextures, value); UpdateTextureListVisibility(_onlyShowUsedTextures); }
        }


        public UiVertexFormat VertexType { get { return _meshNode.Geometry.VertexFormat; } set { ChangeVertexType(value); } }
        public IEnumerable<UiVertexFormat> PossibleVertexTypes { get; set; }

        public MaterialGeneralViewModel(KitbasherRootScene kitbasherRootScene, Rmv2MeshNode meshNode, PackFileService pfs,  ApplicationSettingsService applicationSettings)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _meshNode = meshNode;
            _pfs = pfs;
            _applicationSettingsService = applicationSettings;
            PossibleVertexTypes = new UiVertexFormat[] { UiVertexFormat.Static, UiVertexFormat.Weighted, UiVertexFormat.Cinematic };
            ResolveTexturesCommand = new RelayCommand(ResolveMissingTextures);
            DeleteMissingTexturesCommand = new RelayCommand(DeleteMissingTextures);


            CreateTextureList();
        }

        void CreateTextureList()
        {
            var enumValues = Enum.GetValues(typeof(TextureType)).Cast<TextureType>().ToList();
            var textureEnumValues = _meshNode.Material.GetAllTextures().Select(x => x.TexureType).ToList();
            enumValues.AddRange(textureEnumValues);
            var distinctEnumList = enumValues.Distinct();

            TextureList.Clear();
            foreach (var enumValue in distinctEnumList)
            {
                var textureView = new TextureViewModel(_meshNode, _pfs, enumValue, _applicationSettingsService);
                TextureList.Add(textureView);
            }

            UpdateTextureListVisibility(OnlyShowUsedTextures);
        }

        void UpdateTextureListVisibility(bool onlyShowUsedTextures)
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
                if (onlyShowUsedTextures == false)
                    texture.IsVisible.Value = true;
                else
                    texture.IsVisible.Value = !string.IsNullOrWhiteSpace(texture.Path);
            }
        }

        void ChangeVertexType(UiVertexFormat newFormat)
        {
            var skeletonName = _kitbasherRootScene.Skeleton.SkeletonName;
            _meshNode.Geometry.ChangeVertexType(newFormat, skeletonName);
            NotifyPropertyChanged(nameof(VertexType));
        }

        private void ResolveMissingTextures()
        {
            MissingTextureResolver resolver = new MissingTextureResolver();
            resolver.ResolveMissingTextures(_meshNode, _pfs);
        }

        private void DeleteMissingTextures()
        {
            MissingTextureResolver resolver = new MissingTextureResolver();
            resolver.DeleteMissingTextures(_meshNode, _pfs);
            CreateTextureList();
        }
    }
}
