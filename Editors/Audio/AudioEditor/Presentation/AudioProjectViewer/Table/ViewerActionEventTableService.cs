using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.Core.Events;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectViewer.Table
{
    public class ViewerActionEventTableService(IEventHub eventHub, IAudioEditorStateService audioEditorStateService) : IViewerTableService
    {
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;

        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventType;

        public void Load(DataTable table)
        {
            var schema = DefineSchema();
            ConfigureTable(schema);
            ConfigureDataGrid(schema);
            InitialiseTable(table);
        }

        public List<string> DefineSchema()
        {
            var schema = new List<string>();
            var columnName = TableInfo.EventColumnName;
            schema.Add(columnName);
            return schema;
        }

        public void ConfigureTable(List<string> schema)
        {
            foreach (var columnName in schema)
            {
                var column = new DataColumn(columnName, typeof(string));
                _eventHub.Publish(new ViewerTableColumnAddRequestedEvent(column));
            }
        }

        public void ConfigureDataGrid(List<string> schema)
        {
            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            foreach (var columnName in schema)
            {
                var column = DataGridTemplates.CreateColumnTemplate(columnName, columnWidth, isReadOnly: true);
                column.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnName);
                _eventHub.Publish(new ViewerDataGridColumnAddedEvent(column));
            }
        }

        public void InitialiseTable(DataTable table)
        {
            var actionEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var gameSoundBank = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventName));
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBank}_{audioProjectFileNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                // We don't want them visible as we only show "Play_" Action Events as we force all Action Events to start with "Play_"
                if (actionEvent.Actions.Any(action => action.ActionType == AkActionType.Pause_E_O 
                    || action.ActionType == AkActionType.Resume_E_O
                    || action.ActionType == AkActionType.Stop_E_O))
                    continue;

                var row = table.NewRow();
                row[TableInfo.EventColumnName] = actionEvent.Name;
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            }
        }
    }
}
