using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class EditorStateGroupDataGridService : IDataGridService
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorService _audioEditorService;

        public AudioProjectDataGrid DataGrid => AudioProjectDataGrid.Editor;
        public NodeType NodeType => NodeType.StateGroup;
        public EditorStateGroupDataGridService(IEventHub eventHub, IAudioEditorService audioEditorService)
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
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);
            var column = new DataColumn(columnHeader, typeof(string));
            _eventHub.Publish(new AddEditorTableColumnEvent(column));
        }

        public void ConfigureDataGrid()
        {
            var dataGrid = DataGridHelpers.GetDataGridFromTag(_audioEditorService.AudioProjectEditorViewModel.AudioProjectEditorDataGridTag);
            DataGridHelpers.ClearDataGridColumns(dataGrid);
            DataGridHelpers.ClearDataGridContextMenu(dataGrid);

            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);

            var column = DataGridTemplates.CreateColumnTemplate(columnHeader, 1.0);
            column.CellTemplate = DataGridTemplates.CreateEditableTextBoxTemplate(_eventHub, columnHeader);
            dataGrid.Columns.Add(column);
        }

        public void SetInitialDataGridData(DataTable table)
        {
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var columnHeader = DataGridHelpers.AddExtraUnderscoresToString(stateGroup.Name);

            var row = table.NewRow();
            row[columnHeader] = string.Empty;
            _eventHub.Publish(new AddEditorTableRowEvent(row));
        }
    }
}
