using CommunityToolkit.Mvvm.Input;
using SharedCore.Misc;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class TextureFileEditorServiceViewModel
    {
        TextureFileEditorService _textureService;

        public ICommand CreateProjectCommand { get; set; }
        public ICommand RefreshProjectCommand { get; set; }
        public ICommand RefreshTexturesCommand { get; set; }
        public ICommand OpenFolderCommand { get; set; }
        public ICommand BrowseCommand { get; set; }


        public NotifyAttr<bool> IsRunning { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<string> FilePath { get; set; } = new NotifyAttr<string>("");
        public string FilePrefix { get => _textureService.FilePreFix; set => _textureService.FilePreFix = value; }

        public ObservableCollection<TextureFileEditorService.TextureItem> TextureList { get; set; } = new ObservableCollection<TextureFileEditorService.TextureItem>();

        public TextureFileEditorServiceViewModel(MainEditableNode mainNode)
        {
            CreateProjectCommand = new RelayCommand(() => _textureService.CreateProject());
            RefreshProjectCommand = new RelayCommand(() => _textureService.RefreshProject());
            RefreshTexturesCommand = new RelayCommand(() => _textureService.RefreshTextures());

            OpenFolderCommand = new RelayCommand(() => _textureService.OpenProjectFolder());
            BrowseCommand = new RelayCommand(Browse);

            _textureService = mainNode.TextureFileEditorService;
            _textureService.UpdateStatus();
            UpdateView();
        }


        void UpdateView()
        {
            FilePath.Value = _textureService.ProjectPath;

            TextureList.Clear();
            foreach (var textureItem in _textureService.TextureList)
                TextureList.Add(textureItem);
        }

        void Browse()
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = FilePath.Value;
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            _textureService.SetProjectDirectory(dialog.SelectedPath);
            UpdateView();
        }
    }
}
