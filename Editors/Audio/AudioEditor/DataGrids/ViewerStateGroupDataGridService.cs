using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class ViewerStateGroupDataGridService : IDataGridService
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IEventHub _eventHub;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Viewer;
        public NodeType NodeType => NodeType.StateGroup;

        public ViewerStateGroupDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService)
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
            var columnHeader = DataGridTemplates.StateColumn;
            var column = new DataColumn(columnHeader, typeof(string));
            _eventHub.Publish(new AddViewerTableColumnEvent(column));
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectViewerDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);

            var columnHeader = DataGridTemplates.StateColumn;
            var column = DataGridTemplates.CreateColumnTemplate(columnHeader, 1.0);
            column.CellTemplate = DataGridTemplates.CreateReadOnlyTextBlockTemplate(columnHeader);
            dataGrid.Columns.Add(column);
        }

        public void SetInitialDataGridData(DataTable table)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var columnHeader = DataGridTemplates.StateColumn;

            foreach (var state in stateGroup.States)
            {
                var row = table.NewRow();
                row[columnHeader] = state.Name;
                _eventHub.Publish(new AddViewerTableRowEvent(row));
            }
        }
    }
}
