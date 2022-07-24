using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TextureEditor.ViewModels;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class MaterialGeneralViewModel : NotifyPropertyChangedImpl
    {
        public class TextureViewModel : NotifyPropertyChangedImpl, INotifyDataErrorInfo
        {
            PackFileService _packfileService;
            ApplicationSettingsService _appSettingsService;
            Rmv2MeshNode _meshNode;
            public TexureType TexureType { get; private set; }

            bool _useTexture = true;
            public bool UseTexture { get { return _useTexture; } set { SetAndNotify(ref _useTexture, value); UpdatePreviewTexture(value); } }
            public string TextureTypeStr { get; set; }

            public string Path
            {
                get => _path;
                set
                {
                    if (_path == value)
                        return;

                    _path = value;
                    ValidateTexturePath();
                    NotifyPropertyChanged();

                    UpdateTexturePath(value);
                }
            }

            public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);

            private readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>();
            private string _path = "";

            public TextureViewModel(Rmv2MeshNode meshNode, PackFileService packfileService, TexureType texureType, ApplicationSettingsService appSettingsService)
            {
                _packfileService = packfileService;
                _meshNode = meshNode;
                TexureType = texureType;
                TextureTypeStr = TexureType.ToString();
                _path = _meshNode.Material.GetTexture(texureType)?.Path;
                ValidateTexturePath();
                _appSettingsService = appSettingsService;
            }

            public void Preview() => TexturePreviewController.CreateWindow(Path, _packfileService, _meshNode.Geometry);

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

            private void ValidateTexturePath()
            {
                if (Path == null)
                    return;

                var isFileFound = true;
                
                if(string.IsNullOrWhiteSpace(Path) == false)
                    isFileFound = _packfileService.FindFile(Path) != null;

                if (isFileFound == false && Path.Contains("test_mask.dds"))
                    isFileFound = true;

                if (isFileFound == false)
                {
                    var errorMessage = "Invalid Texture Path!";
                    _errorsByPropertyName[nameof(Path)] = new List<string>() {errorMessage};
                }
                else
                {
                    _errorsByPropertyName[nameof(Path)] = null;
                }

                OnErrorsChanged(nameof(Path));
            }

            public void Remove()
            {
                UseTexture = false;
                UpdateTexturePath("");
            }

            void UpdateTexturePath(string newPath)
            {
                Path = newPath;
                _meshNode.UpdateTexture(Path, TexureType);
            }

            void UpdatePreviewTexture(bool value)
            {
                _meshNode.UseTexture(TexureType, value);
            }

            public IEnumerable GetErrors(string propertyName)
            {
                return _errorsByPropertyName.ContainsKey(propertyName) ?
                    _errorsByPropertyName[propertyName] : null;
            }

            public bool HasErrors => _errorsByPropertyName.Any();
            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

            private void OnErrorsChanged(string propertyName)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        Rmv2MeshNode _meshNode;
        IComponentManager _componentManager;
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
            set { SetAndNotify(ref _onlyShowUsedTextures, value); UpdateTextureListVisibility(_onlyShowUsedTextures); } }


        public UiVertexFormat VertexType { get { return _meshNode.Geometry.VertexFormat; } set { ChangeVertexType(value); } }
        public IEnumerable<UiVertexFormat> PossibleVertexTypes { get; set; }

        public MaterialGeneralViewModel(Rmv2MeshNode meshNode, PackFileService pfs, IComponentManager componentManager, ApplicationSettingsService applicationSettings)
        {
            _componentManager = componentManager;
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
            var enumValues = Enum.GetValues(typeof(TexureType)).Cast<TexureType>().ToList();
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
                if(_applicationSettingsService.CurrentSettings.HideWh2TextureSelectors && 
                   _applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3)
                {
                    if(texture.TexureType == TexureType.Diffuse  ||
                       texture.TexureType == TexureType.Specular ||
                       texture.TexureType == TexureType.Gloss)
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
            var mainNode = _componentManager.GetComponent<IEditableMeshResolver>();
            var skeletonName = mainNode.GeEditableMeshRootNode()?.Skeleton.Name;
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
