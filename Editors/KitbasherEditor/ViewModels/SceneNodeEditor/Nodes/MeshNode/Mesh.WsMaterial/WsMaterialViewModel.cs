using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.SceneNodes;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.Types;
using Shared.Ui.BaseDialogs.MathViews;
using Shared.Ui.Events.UiCommands;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews
{
    public partial class WsMaterialViewModel : ObservableObject
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        [ObservableProperty] WsMaterialShared_ViewModel _shared;
        [ObservableProperty] WsMaterialBlood_ViewModel _blood;

        public WsMaterialViewModel(IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _uiCommandFactory = uiCommandFactory;
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        internal void Initialize(Rmv2MeshNode typedNode)
        {
            Shared = new WsMaterialShared_ViewModel(typedNode, _uiCommandFactory, _packFileService, _resourceLibrary);
            Blood = new WsMaterialBlood_ViewModel(typedNode, _uiCommandFactory, _packFileService, _resourceLibrary);
        }
    }

    public partial class WsMaterialBlood_ViewModel : ObservableObject
    {
        private readonly BloodCapability _bloodCapability;

        [ObservableProperty] bool _useBlood;
        [ObservableProperty] ShaderTextureViewModel _bloodMap;
        [ObservableProperty] Vector2ViewModel _bloodUvScale = new(0,0);
        [ObservableProperty] DoubleViewModel _bloodPreview = new();

        public WsMaterialBlood_ViewModel(Rmv2MeshNode typedNode, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            var bloodCapability = typedNode.Effect.GetCapability<BloodCapability>();
            Guard.IsNotNull(bloodCapability);
            _bloodCapability = bloodCapability;

            _bloodMap = new ShaderTextureViewModel(TextureType.Blood, bloodCapability, packFileService, uiCommandFactory, resourceLibrary);
            _useBlood = _bloodCapability.UseBlood;
            _bloodUvScale = new Vector2ViewModel(_bloodCapability.UvScale.X, _bloodCapability.UvScale.Y);
            _bloodPreview = new DoubleViewModel(_bloodCapability.PreviewBlood);
        }

        partial void OnBloodUvScaleChanged(Vector2ViewModel value) => _bloodCapability.UvScale = value.GetAsVector2();
        partial void OnUseBloodChanged(bool value) => _bloodCapability.UseBlood = value;
        partial void OnBloodPreviewChanged(DoubleViewModel value) => _bloodCapability.PreviewBlood = (float)value.Value;
    }

    public partial class WsMaterialShared_ViewModel : ObservableObject
    {
        private readonly DefaultCapability _defaultCapability;

        [ObservableProperty] bool _useAlpha;
        [ObservableProperty] ShaderTextureViewModel _baseColour;
        [ObservableProperty] ShaderTextureViewModel _materialMap;
        [ObservableProperty] ShaderTextureViewModel _normalMap;
        [ObservableProperty] ShaderTextureViewModel _mask;

        public WsMaterialShared_ViewModel(Rmv2MeshNode typedNode, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
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

    public partial class ShaderTextureViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private readonly TextureType _textureType;
        private readonly ITextureCapability _textureCapability;

        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly ResourceLibrary _resourceLibrary;

        [ObservableProperty] string _path;
        [ObservableProperty] bool _isEnabled;

        public ShaderTextureViewModel(TextureType textureType, ITextureCapability textureCapability, PackFileService packFileService, IUiCommandFactory uiCommandFactory, ResourceLibrary resourceLibrary) 
        {
            var supportsTexture = textureCapability.SupportsTexture(textureType);
            if (supportsTexture == false)
                throw new Exception($"Provided textureCapability {textureCapability.GetType()} does not support {textureType}");

            _textureType = textureType;
            _textureCapability = textureCapability;
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
            _resourceLibrary = resourceLibrary;
            _path = textureCapability.GetTexturePath(textureType);
            _isEnabled = _textureCapability.GetTextureUsage(textureType);
        }

        partial void OnIsEnabledChanged(bool value)
        {
            _textureCapability.SetTextureUsage(_textureType, value);
        }

        partial void OnPathChanged(string value)
        {
            _textureCapability.SetTexturePath(_textureType, value);
            ValidatePath();
        }


        [RelayCommand]
        void PreviewTextureCommand(TextureType textureType)
        {
            if (HasErrors == false)
                _uiCommandFactory.Create<OpenFileInWindowedEditorCommand>().Execute(Path, 800, 900);
        }

        [RelayCommand]
        void BrowseTextureCommand(TextureType textureType)
        {
            using var browser = new PackFileBrowserWindow(_packFileService);
            browser.ViewModel.Filter.SetExtentions([".dds", ".png",]);
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                try
                {
                    var path = _packFileService.GetFullPath(browser.SelectedFile);
                    _resourceLibrary.LoadTexture(path);

                    Path = path;
                    IsEnabled = true;
                }
                catch
                {
                    MessageBox.Show($"Failed to load texture {browser.SelectedFile}");
                    IsEnabled = false;
                }
            }
        }

        [RelayCommand]
        void ClearTextureCommand(TextureType textureType)
        {
            Path = string.Empty;
        }

        void ValidatePath()
        {
            _errorsByPropertyName[nameof(Path)] = new List<string>();
            if (Path == null || string.IsNullOrWhiteSpace(Path) == false)
            {
                _errorsByPropertyName[nameof(Path)].Add("Path is required. If none is wanted, use 'test_mask.dds'");
            }
            else
            {
                if (Path.Contains("test_mask.dds") == false)
                {
                    var isFileFound = _packFileService.FindFile(Path) != null;
                    if (isFileFound == false)
                    {
                        _errorsByPropertyName[nameof(Path)].Add("Invalid texture path. Path is not found in loaded packfiles");
                    }
                }
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Path)));
        }

        private readonly Dictionary<string, List<string>> _errorsByPropertyName = [];
        public bool HasErrors => _errorsByPropertyName.Any();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return Enumerable.Empty<string>();

            if (_errorsByPropertyName.ContainsKey(propertyName))
                return _errorsByPropertyName[propertyName];

            return Enumerable.Empty<string>();
        }

    }
}
