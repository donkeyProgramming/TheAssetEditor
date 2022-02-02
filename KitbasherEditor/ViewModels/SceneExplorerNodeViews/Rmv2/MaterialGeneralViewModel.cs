using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
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
using CommonControls.FileTypes.PackFiles.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using TextureEditor.ViewModels;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class MaterialGeneralViewModel : NotifyPropertyChangedImpl
    {
        public class TextureViewModel : NotifyPropertyChangedImpl, INotifyDataErrorInfo
        {
            ILogger _logger = Logging.Create<TextureViewModel>();

            PackFileService _packfileService;
            Rmv2MeshNode _meshNode;
            TexureType _texureType;

            FileSystemWatcher _watcher;

            public ICommand SaveWatchedCommand { get; set; }
            public ICommand ExportAndWatchCommand { get; set; }

            bool _useTexture = true;
            public bool UseTexture { get { return _useTexture; } set { SetAndNotify(ref _useTexture, value); UpdatePreviewTexture(value); } }
            public string TextureTypeStr { get; set; }

            public string Path
            {
                get => _path;
                set
                {
                    var isNewViewModel = string.IsNullOrEmpty(_path);

                    _path = value?.Replace("/", "\\");
                    ValidateTexturePath();
                    NotifyPropertyChanged();

                    UpdateMeshTexture(_path, !isNewViewModel);

                    IsWatched = File.Exists(_path);
                }
            }

            public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);

            public bool IsWatched
            {
                get => _isWatched;
                set
                {
                    _isWatched = value;
                    NotifyPropertyChanged();

                    CanSaveWatchedCommand = value;
                    CanExportAndWatchCommand = !value;

                    if (_watcher != null)
                    {
                        _watcher.Changed -= WatcherOnChanged;
                        _watcher.Deleted -= WatcherOnDeleted;
                        _watcher.Dispose();
                    }

                    if (value)
                    {
                        var watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(Path));
                        watcher.NotifyFilter = NotifyFilters.Attributes
                                               | NotifyFilters.CreationTime
                                               | NotifyFilters.DirectoryName
                                               | NotifyFilters.FileName
                                               | NotifyFilters.LastAccess
                                               | NotifyFilters.LastWrite
                                               | NotifyFilters.Security
                                               | NotifyFilters.Size;
                        watcher.Changed += WatcherOnChanged;
                        watcher.Deleted += WatcherOnDeleted;
                        watcher.Filter = System.IO.Path.GetFileName(Path);
                        watcher.EnableRaisingEvents = true;

                        _watcher = watcher;
                    }
                }
            }

            private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
            {
                Path = Path; // force validation
            }

            public bool CanSaveWatchedCommand
            {
                get => _canSaveWatchedCommand && _errorsByPropertyName[nameof(Path)] == null;
                set
                {
                    _canSaveWatchedCommand = value;
                    NotifyPropertyChanged();
                }
            }

            public bool CanExportAndWatchCommand
            {
                get => _canExportAndWatchCommand && _errorsByPropertyName[nameof(Path)] == null;
                set
                {
                    _canExportAndWatchCommand = value;
                    NotifyPropertyChanged();
                }
            }

            private readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>();
            private string _path = "";
            private bool _isWatched;
            private bool _canSaveWatchedCommand;
            private bool _canExportAndWatchCommand;

            public TextureViewModel(Rmv2MeshNode meshNode, PackFileService packfileService, TexureType texureType)
            {
                _packfileService = packfileService;
                _meshNode = meshNode;
                _texureType = texureType;
                TextureTypeStr = _texureType.ToString();
                Path = _meshNode.Material.GetTexture(texureType)?.Path;

                SaveWatchedCommand = new RelayCommand(SaveWatched);
                ExportAndWatchCommand = new RelayCommand(ExportAndWatch);
            }

            public void Preview() => TexturePreviewController.CreateWindow(Path, _packfileService);

            public void Import()
            {
                var dialog = new CommonOpenFileDialog();
                dialog.Filters.Add(new CommonFileDialogFilter("DDS", ".dds,.png"));
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var fileName = dialog.FileName;
                    _logger.Here().Information($"Loading pack file {fileName}");

                    if (System.IO.Path.GetExtension(fileName) == ".dds")
                    {
                        SaveHelper.SaveDDSTextureAsPNG(fileName);
                    }

                    Path = System.IO.Path.ChangeExtension(fileName, ".png");
                }
            }

            private void WatcherOnChanged(object sender, FileSystemEventArgs e)
            {
                if (e.ChangeType != WatcherChangeTypes.Changed)
                {
                    return;
                }

                // using the Dispatcher so we can use WaitCursor later
                Application.Current.Dispatcher.Invoke(() => UpdateMeshTexture(Path, true));
                _logger.Here().Information($"Updating texture {Path}");
            }

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

                if (File.Exists(Path))
                {
                    _errorsByPropertyName[nameof(Path)] = null;
                    return;
                }

                var path = Path.Replace("/", @"\");

                if (!_packfileService.Database.PackFiles.Any(pf => pf.FileList.Any(pair => pair.Key.Equals(path, StringComparison.OrdinalIgnoreCase))))
                {
                    var errorMessage = "Invalid Texture Path!" +
                                       (TextureTypeStr == "Mask" ? " This is fine on mask textures." : "");
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
                UpdateMeshTexture(newPath);
            }

            void UpdateMeshTexture(string newPath, bool skipCache = false)
            {
                _meshNode.UpdateTexture(newPath, _texureType, skipCache);
            }

            void UpdatePreviewTexture(bool value)
            {
                _meshNode.UseTexture(_texureType, value);
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

            private void ExportAndWatch()
            {
                var dialog = new CommonSaveFileDialog
                {
                    Title = "Export Texture",
                    DefaultExtension = "dds",
                    DefaultFileName = System.IO.Path.GetFileNameWithoutExtension(Path),
                    Filters = {new CommonFileDialogFilter("DDS", ".dds")},
                    IsExpandedMode = true,
                };
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var newPath = dialog.FileName;

                    // write the DDS texture
                    File.WriteAllBytes(newPath, _packfileService.FindFile(Path).DataSource.ReadData());

                    // also save a PNG version of the texture for the user to edit
                    SaveHelper.SaveDDSTextureAsPNG(newPath);

                    var pngPath = System.IO.Path.ChangeExtension(newPath, ".png");
                    Path = pngPath;
                }
            
            }

            private void SaveWatched()
            {
                var ddsExists = System.IO.Path.GetExtension(Path) == ".png" && File.Exists(System.IO.Path.ChangeExtension(Path, ".dds"));

                var newFilePath = "";
                var extention = ddsExists ? ".dds" : System.IO.Path.GetExtension(Path);
                using (var browser = new SavePackFileWindow(_packfileService))
                {
                    browser.ResetNameOnFolderSelect = false;
                    browser.CurrentFileName = System.IO.Path.GetFileName(ddsExists ? System.IO.Path.ChangeExtension(Path, ".dds") : Path);
                    if (browser.ShowDialog() == true)
                    {
                        var browserPath = browser.FilePath;
                        if (!browserPath.Contains(extention))
                            browserPath += extention;

                        newFilePath = browserPath;
                    }
                    else
                    {
                        return;
                    }
                }

                var directoryPath = System.IO.Path.GetDirectoryName(newFilePath);
                if (string.IsNullOrEmpty(newFilePath))
                    return;

                var path = Path;
                if (ddsExists)
                {
                    path = System.IO.Path.ChangeExtension(path, ".dds");
                    newFilePath = System.IO.Path.ChangeExtension(newFilePath, ".dds");
                }

                var selectedEditabelPackFile = _packfileService.GetEditablePack();
                var newPackFile = new PackFile(System.IO.Path.GetFileName(newFilePath), new MemorySource(File.ReadAllBytes(path)));
                _packfileService.AddFileToPack(selectedEditabelPackFile, directoryPath, newPackFile);

                Path = newFilePath;
            }
        }

        Rmv2MeshNode _meshNode;
        IComponentManager _componentManager;

        public bool UseAlpha {
            get { return _meshNode.Material.AlphaMode == AlphaMode.Transparent; } 
            set 
            {   if (value)
                    _meshNode.Material.AlphaMode = AlphaMode.Transparent;
                else
                     _meshNode.Material.AlphaMode = AlphaMode.Opaque;
                NotifyPropertyChanged();
            } 
        }
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
    }
}
