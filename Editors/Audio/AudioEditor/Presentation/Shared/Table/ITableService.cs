using System.Collections.Generic;
using System.Data;

namespace Editors.Audio.AudioEditor.Presentation.Shared.Table
{
    public interface ITableService
    {
        AudioProjectTreeNodeType NodeType { get; }
        void Load(DataTable table);
        List<string> DefineSchema();
        void ConfigureTable(List<string> schema);
        void ConfigureDataGrid(List<string> schema);
        void InitialiseTable(DataTable table);
    }

    public interface IEditorTableService : ITableService { }

    public interface IViewerTableService : ITableService { }
}
