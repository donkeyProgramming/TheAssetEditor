using System.Collections;
using System.ComponentModel;
using System.Windows;
using CommonControls.PackFileBrowser;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.Types;
using Shared.Ui.Events.UiCommands;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public partial class MaterialGeneralViewModel
    {
        public class TextureViewModel : NotifyPropertyChangedImpl, INotifyDataErrorInfo
        {
            private readonly PackFileService _packfileService;
            private readonly Rmv2MeshNode _meshNode;
            private readonly IUiCommandFactory _uiCommandFactory;

            public TextureType TexureType { get; private set; }

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

            public TextureViewModel(Rmv2MeshNode meshNode, TextureType texureType, PackFileService packfileService, IUiCommandFactory uiCommandFactory)
            {
                _packfileService = packfileService;
                _uiCommandFactory = uiCommandFactory;
                _meshNode = meshNode;
                TexureType = texureType;
                TextureTypeStr = TexureType.ToString();
                _path = _meshNode.Material.GetTexture(texureType)?.Path;
                ValidateTexturePath();
            }

            public void Paste()
            {
                var path = Clipboard.GetText();
                if (_packfileService.FindFile(path) == null)
                {
                    MessageBox.Show($"Invalid path or path not found {path}", "Attention", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                UpdateTexturePath(path);

            }
            public void Preview()
            {
                _uiCommandFactory.Create<OpenFileInWindowedEditorCommand>().Execute(Path, 800, 900);
            }


            public void Browse()
            {
                using var browser = new PackFileBrowserWindow(_packfileService);
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

            private void ValidateTexturePath()
            {
                if (Path == null)
                    return;

                var isFileFound = true;

                if (string.IsNullOrWhiteSpace(Path) == false)
                    isFileFound = _packfileService.FindFile(Path) != null;

                if (isFileFound == false && Path.Contains("test_mask.dds"))
                    isFileFound = true;

                if (isFileFound == false)
                {
                    var errorMessage = "Invalid Texture Path!";
                    _errorsByPropertyName[nameof(Path)] = new List<string>() { errorMessage };
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
    }
}
