using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;

namespace Editors.Audio.AudioEditor.Presentation.Table
{
    public interface ITableService
    {
        AudioProjectExplorerTreeNodeType NodeType { get; }
        void Load(DataTable table);
        List<string> DefineSchema();
        void ConfigureTable(List<string> schema);
        void ConfigureDataGrid(List<string> schema);
        void InitialiseTable(DataTable table);
    }

    public interface IEditorTableService : ITableService { }
    public interface IViewerTableService : ITableService { }
}
