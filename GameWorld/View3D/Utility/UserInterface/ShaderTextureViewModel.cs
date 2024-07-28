using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.Types;
using Shared.Ui.Events.UiCommands;

namespace GameWorld.Core.Utility.UserInterface
{
    public partial class ShaderTextureViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private readonly TextureType _textureType;
        private readonly ITextureCapability _textureCapability;

        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly ResourceLibrary _resourceLibrary;

        [ObservableProperty] string _path;
        [ObservableProperty] bool _shouldRenderTexture;

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
            _shouldRenderTexture = _textureCapability.GetTextureUsage(textureType);
        }

        partial void OnShouldRenderTextureChanged(bool value)
        {
            _textureCapability.SetTextureUsage(_textureType, value);
        }

        partial void OnPathChanged(string value)
        {
            _textureCapability.SetTexturePath(_textureType, value);
            ValidatePath();
        }

        [RelayCommand]
        void HandlePreviewTexture()
        {
            if (HasErrors == false)
                _uiCommandFactory.Create<OpenFileInWindowedEditorCommand>().Execute(Path, 800, 900);
        }

        [RelayCommand]
        void HandleBrowseTexture()
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
                    ShouldRenderTexture = true;
                }
                catch
                {
                    MessageBox.Show($"Failed to load texture {browser.SelectedFile}");
                    ShouldRenderTexture = false;
                }
            }
        }

        [RelayCommand]
        void HandleClearTexture()
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
