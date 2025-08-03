using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Events;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class EditViewerRowCommand(
        IAudioEditorService audioEditorService,
        IUiCommandFactory uiCommandFactory,
        IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IUiCommandFactory _uiCommandFactory = uiCommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        private readonly ILogger _logger = Logging.Create<EditViewerRowCommand>();

        public void Execute(List<DataRow> selectedViewerRows)
        {
            // Publish before removing to ensure that an item is still selected
            _eventHub.Publish(new ViewerTableRowEditedEvent(selectedViewerRows[0]));

            _uiCommandFactory.Create<RemoveViewerRowsCommand>().Execute(selectedViewerRows);

            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            _logger.Here().Information($"Editing {selectedAudioProjectExplorerNode.NodeType} row in Audio Project Viewer table for {selectedAudioProjectExplorerNode.Name}");
        }
    }
}
