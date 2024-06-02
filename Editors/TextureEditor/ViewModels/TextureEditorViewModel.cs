using System;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace TextureEditor.ViewModels
{
    public class TextureEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel, IDisposable
    {
        PackFileService _pfs;
        PackFile _file;
        TexturePreviewController _controller;
        EventHub _eventHub;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>();

        public PackFile MainFile { get => _file; set => Load(value); }
        public bool HasUnsavedChanges { get => false; set { } }


        TexturePreviewViewModel _viewModel;
        public TexturePreviewViewModel ViewModel
        {
            get => _viewModel;
            set => SetAndNotify(ref _viewModel, value);
        }

        public TextureEditorViewModel(PackFileService pfs, EventHub eventHub)
        {
            _pfs = pfs;
            _eventHub = eventHub;
        }

        public void Load(PackFile file)
        {
            _file = file;
            DisplayName.Value = file.Name;

            var viewModel = new TexturePreviewViewModel();
            viewModel.ImagePath.Value = _pfs.GetFullPath(file);

            _controller = new TexturePreviewController(_pfs.GetFullPath(file), viewModel, _pfs, _eventHub);
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
