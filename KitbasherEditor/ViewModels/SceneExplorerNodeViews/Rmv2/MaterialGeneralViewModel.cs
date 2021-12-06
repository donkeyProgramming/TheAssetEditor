using Common;
using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TextureEditor.ViewModels;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class MaterialGeneralViewModel : NotifyPropertyChangedImpl
    {
        public class TextureViewModel : NotifyPropertyChangedImpl
        {
            PackFileService _packfileService;
            Rmv2MeshNode _meshNode;
            TexureType _texureType;

            bool _useTexture = true;
            public bool UseTexture { get { return _useTexture; } set { SetAndNotify(ref _useTexture, value); UpdatePreviewTexture(value); } }
            public string TextureTypeStr { get; set; }

            public NotifyAttr<string> Path { get; set; } = new NotifyAttr<string>("");

            public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);

            public TextureViewModel(Rmv2MeshNode meshNode, PackFileService packfileService, TexureType texureType)
            {
                _packfileService = packfileService;
                _meshNode = meshNode;
                _texureType = texureType;
                Path.Value = _meshNode.Material.GetTexture(texureType)?.Path;
                TextureTypeStr = _texureType.ToString();
            }


            public void Preview() => TexturePreviewController.CreateVindow(Path.Value, _packfileService);

            public void Browse()
            {
                using (var browser = new PackFileBrowserWindow(_packfileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".dds", ".png", });
                    if (browser.ShowDialog() == true && browser.SelectedFile != null)
                    {
                        try
                        {
                            var path = _packfileService.GetFullPath(browser.SelectedFile);
                            UpdateTexturePath(path);  
                        }
                        catch
                        {
                            UpdatePreviewTexture(false);
                        }
                    }
                }
            }

            public void Remove()
            {
                UseTexture = false;
                UpdateTexturePath("");
            }

            void UpdateTexturePath(string newPath)
            {
                Path.Value = newPath;

                _meshNode.UpdateTexture(Path.Value, _texureType);
            }

            void UpdatePreviewTexture(bool value)
            {
                _meshNode.UseTexture(_texureType, value);
            }
        }

        Rmv2MeshNode _meshNode;
        IComponentManager _componentManager;

        public AlphaMode AlphaModeValue { get { return _meshNode.Material.AlphaMode; } set { _meshNode.Material.AlphaMode = value; NotifyPropertyChanged(); } }
        public IEnumerable<AlphaMode> PossibleAlphaModes { get; set; } = new List<AlphaMode>() { AlphaMode.Opaque, AlphaMode.Alpha_Test, AlphaMode.Alpha_Blend };

        public string TextureDirectory { get { return _meshNode.Material.TextureDirectory; } set { _meshNode.Material.TextureDirectory = value; NotifyPropertyChanged(); } }
        public ObservableCollection<TextureViewModel> TextureList { get; set; } = new ObservableCollection<TextureViewModel>();


        bool _onlyShowUsedTextures = true;
        public bool OnlyShowUsedTextures { get { return _onlyShowUsedTextures; } set { SetAndNotify(ref _onlyShowUsedTextures, value); UpdateTextureListVisibility(value); } }

        public UiVertexFormat VertexType { get { return _meshNode.Geometry.VertexFormat; } set { ChangeVertexType(value); } }
        public IEnumerable<UiVertexFormat> PossibleVertexTypes { get; set; }

        public MaterialGeneralViewModel(Rmv2MeshNode meshNode, PackFileService pf, IComponentManager componentManager)
        {
            _componentManager = componentManager;
            _meshNode = meshNode;
            PossibleVertexTypes = new UiVertexFormat[] { UiVertexFormat.Static, UiVertexFormat.Weighted, UiVertexFormat.Cinematic };
     
            var enumValues = Enum.GetValues(typeof(TexureType)).Cast<TexureType>().ToList();
            var textureEnumValues = _meshNode.Material.GetAllTextures().Select(x => x.TexureType).ToList();
            enumValues.AddRange(textureEnumValues);
            var distinctEnumList = enumValues.Distinct();

            foreach (var enumValue in distinctEnumList)
            {
                var textureView = new TextureViewModel(_meshNode, pf, enumValue);
                TextureList.Add(textureView);
            }

            UpdateTextureListVisibility(OnlyShowUsedTextures);
        }

        void UpdateTextureListVisibility(bool onlyShowUsedTextures)
        {
            foreach (var texture in TextureList)
            {
                if (onlyShowUsedTextures == false)
                    texture.IsVisible.Value = true;
                else
                    texture.IsVisible.Value = !string.IsNullOrWhiteSpace(texture.Path.Value);
            }
        }

        void ChangeVertexType(UiVertexFormat newFormat)
        {
            var mainNode = _componentManager.GetComponent<IEditableMeshResolver>();
            var skeletonName = mainNode.GeEditableMeshRootNode()?.Skeleton.Name;
            _meshNode.Geometry.ChangeVertexType(newFormat, skeletonName);
            NotifyPropertyChanged(nameof(VertexType));
        }
    }
}
