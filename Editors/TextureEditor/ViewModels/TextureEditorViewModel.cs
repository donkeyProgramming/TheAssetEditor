using System;
using GameWorld.WpfWindow;
using Monogame.WpfInterop.ResourceHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace TextureEditor.ViewModels
{
    public class TextureEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel, IDisposable
    {
        private readonly PackFileService _pfs;

        private readonly WpfGame _wpfGame;
        private readonly ResourceLibrary _resourceLibrary;

        TexturePreviewController _controller;

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

        public TextureEditorViewModel(PackFileService pfs,  WpfGame wpfGame, ResourceLibrary resourceLibrary)
        {
            _pfs = pfs;
            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
        }

        private void Load(PackFile packFile)
        {
            _packFile = packFile;
            _wpfGame.ForceCreate();
            DisplayName.Value = _packFile.Name;

            var viewModel = new TexturePreviewViewModel();
            viewModel.ImagePath.Value = _pfs.GetFullPath(_packFile);

            _controller = new TexturePreviewController(viewModel, _resourceLibrary, _wpfGame);
            _controller.Build(_pfs.GetFullPath(_packFile));
            ViewModel = viewModel;
        }

        public void ShowTextureDetailsInfo() => ViewModel.ShowTextureDetailsInfo();

        public void Close()
        {

        }

        public bool Save() => false;

        public void Dispose()
        {
            if (_controller != null)
                _controller.Dispose();
        }
    }
}
