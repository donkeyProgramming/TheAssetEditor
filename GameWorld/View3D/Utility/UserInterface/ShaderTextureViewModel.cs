using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Services;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.Events.UiCommands;

namespace GameWorld.Core.Utility.UserInterface
{
    public partial class ShaderTextureViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private readonly TextureInput _shaderTextureReference;
        private readonly IPackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IScopedResourceLibrary _resourceLibrary;
        private readonly IStandardDialogs _packFileUiProvider;

        [ObservableProperty] string _path;
        [ObservableProperty] bool _shouldRenderTexture;

        public ShaderTextureViewModel(TextureInput shaderTextureReference, IPackFileService packFileService, IUiCommandFactory uiCommandFactory, IScopedResourceLibrary resourceLibrary, IStandardDialogs packFileUiProvider) 
        {
            _shaderTextureReference = shaderTextureReference;
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
            _resourceLibrary = resourceLibrary;
            _packFileUiProvider = packFileUiProvider;
            Path = _shaderTextureReference.TexturePath;
            _shouldRenderTexture = _shaderTextureReference.UseTexture;
        }

        partial void OnShouldRenderTextureChanged(bool value) 
        {
            _shaderTextureReference.UseTexture = value;
        }

        partial void OnPathChanged(string value)
        {
            _shaderTextureReference.TexturePath = value;
            ValidatePath();
        }

        [RelayCommand]
        void HandlePreviewTexture()
        {
            if (HasErrors == false)
                _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(Path, 800, 900);
        }

        [RelayCommand]
        void HandleBrowseTexture()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".dds", ".png"]);
            if (result.Result == true && result.File != null)
            {
                try
                {
                    var path = _packFileService.GetFullPath(result.File);
                    _resourceLibrary.LoadTexture(path);

                    Path = path;
                    ShouldRenderTexture = true;
                }
                catch
                {
                    MessageBox.Show($"Failed to load texture {result.File}");
                    ShouldRenderTexture = false;
                }
            }
        }

        [RelayCommand]
        void HandleClearTexture()
        {
            Path = "test_mask.dds";
        }

        void ValidatePath()
        {
            _errorsByPropertyName[nameof(Path)] = new List<string>();
            if (string.IsNullOrWhiteSpace(Path))
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
        public bool HasErrors => _errorsByPropertyName.Sum(x=>x.Value.Count) != 0;

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
