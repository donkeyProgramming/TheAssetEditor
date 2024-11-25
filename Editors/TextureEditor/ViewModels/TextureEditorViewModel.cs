using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.TextureEditor.ViewModels
{
    public class TextureEditorViewModel : NotifyPropertyChangedImpl, IEditorInterface, IFileEditor
    {
        private readonly IPackFileService _pfs;
        private readonly TextureBuilder _textureBuilder;

        public string DisplayName { get; set; } = "Not set";

        PackFile _packFile;
        public PackFile CurrentFile => _packFile;

        TexturePreviewViewModel _viewModel; // UseNotify
        public TexturePreviewViewModel ViewModel
        {
            get => _viewModel;
            set => SetAndNotify(ref _viewModel, value);
        }



        public TextureEditorViewModel(IPackFileService pfs, TextureBuilder textureBuilder)
        {
            _pfs = pfs;
            _textureBuilder = textureBuilder;
        }

        public void LoadFile(PackFile packFile)
        {
            _packFile = packFile;

            DisplayName = _packFile.Name;

            var viewModel = new TexturePreviewViewModel();
            viewModel.ImagePath.Value = _pfs.GetFullPath(_packFile);

            _textureBuilder.Build(viewModel, _pfs.GetFullPath(_packFile));
            ViewModel = viewModel;
        }

        public void ShowTextureDetailsInfo() => ViewModel.ShowTextureDetailsInfo();

        public void Close()
        {

        }
    }
}
