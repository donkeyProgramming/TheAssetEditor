using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace TextureEditor.ViewModels
{
    public class TextureEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        private readonly PackFileService _pfs;
        private readonly TextureBuilder _textureBuilder;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>();

        PackFile _packFile;
        public PackFile MainFile { get => _packFile; set => Load(value); }  // Change to own interface or function

        public bool HasUnsavedChanges { get => false; set { } } // Move to own interface


        TexturePreviewViewModel _viewModel; // UseNotify
        public TexturePreviewViewModel ViewModel
        {
            get => _viewModel;
            set => SetAndNotify(ref _viewModel, value);
        }

        public TextureEditorViewModel(PackFileService pfs, TextureBuilder textureBuilder)
        {
            _pfs = pfs;
            _textureBuilder = textureBuilder;
        }

        private void Load(PackFile packFile)
        {
            _packFile = packFile;
 
            DisplayName.Value = _packFile.Name;

            var viewModel = new TexturePreviewViewModel();
            viewModel.ImagePath.Value = _pfs.GetFullPath(_packFile);

            _textureBuilder.Build(viewModel, _pfs.GetFullPath(_packFile));
            ViewModel = viewModel;
        }
          
        public void ShowTextureDetailsInfo() => ViewModel.ShowTextureDetailsInfo();

        public void Close()
        {

        }

        public bool Save() => false;
    }
}
