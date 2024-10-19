using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;

namespace Shared.Ui.Events.UiCommands
{
    public class OpenFileInWindowedEditorCommand : IUiCommand
    {
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public OpenFileInWindowedEditorCommand(IToolFactory toolFactory, PackFileService packFileService)
        {
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public void Execute(string fileName, int width, int heigh)
        {
            var viewModel = _toolFactory.Create(fileName, true);
            if(viewModel is IFileEditor fileEditor)
                fileEditor.LoadFile(_packFileService.FindFile(fileName));

            var window = _toolFactory.CreateAsWindow(viewModel);
            window.Width = width;
            window.Height = heigh;
            window.Title = viewModel.DisplayName;
            window.ShowDialog();
        }
    }
}
