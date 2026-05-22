using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectViewer
{
    public class EditViewerRowsCommand(
        IAudioEditorStateService audioEditorStateService,
        IUiCommandFactory uiCommandFactory,
        IEventHub eventHub) : IAeCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IUiCommandFactory _uiCommandFactory = uiCommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        private readonly ILogger _logger = Logging.Create<EditViewerRowsCommand>();
        private List<DataRow> _rows = new();

        public void Configure(List<DataRow> rows)
        {
            _rows = rows;
        }

        public void Execute()
        {
            // Publish before removing to ensure that an item is still selected
            _eventHub.Publish(new ViewerTableRowEditedEvent(_rows[0]));

            _uiCommandFactory.Create<RemoveViewerRowsCommand>(x => x.Configure(_rows)).Execute();

            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            _logger.Here().Information($"Editing {selectedAudioProjectExplorerNode.Type} row in Audio Project Viewer table for {selectedAudioProjectExplorerNode.Name}");
        }
    }
}
