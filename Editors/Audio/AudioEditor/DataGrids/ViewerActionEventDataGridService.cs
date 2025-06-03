using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class ViewerActionEventDataGridService : IDataGridService
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Viewer;
        public NodeType NodeType => NodeType.ActionEventSoundBank;

        public ViewerActionEventDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService)
        {
            _eventHub = eventHub;
            _audioEditorService = audioEditorService;
        }

        public void LoadDataGrid(DataTable table)
        {
            SetTableSchema();
            ConfigureDataGrid();
            SetInitialDataGridData(table);
        }

        public void SetTableSchema()
        {
            var columnHeader = DataGridTemplates.EventNameColumn;
            var column = new DataColumn(columnHeader, typeof(string));
            _eventHub.Publish(new AddViewerTableColumnEvent(column));
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectViewerDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);

            var columnHeader = DataGridTemplates.EventNameColumn;
            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridTemplates.CreateColumnTemplate(columnHeader, columnWidth, isReadOnly: true);
            eventColumn.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnHeader);
            dataGrid.Columns.Add(eventColumn);
        }

        public void SetInitialDataGridData(DataTable table)
        {
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var row = table.NewRow();
                row[DataGridTemplates.EventNameColumn] = actionEvent.Name;
                _eventHub.Publish(new AddViewerTableRowEvent(row));
            }
        }
    }
}
