using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public enum AudioProjectDataGrid
    {
        Editor,
        Viewer
    }

    public interface IDataGridService
    {
        AudioProjectDataGrid DataGrid { get; }
        NodeType NodeType { get; }
        public void LoadDataGrid(DataTable table);
        public void SetTableSchema();
        public void ConfigureDataGrid();
        public void SetInitialDataGridData(DataTable table);
    }

    public interface IDataGridServiceFactory
    {
        IDataGridService GetService(AudioProjectDataGrid dataGrid, NodeType nodeType);
    }

    public class DataGridServiceFactory : IDataGridServiceFactory
    {
        private readonly Dictionary<(AudioProjectDataGrid dataGrid, NodeType nodeType), IDataGridService> _services;

        public DataGridServiceFactory(IUiCommandFactory uiCommandFactory, IEventHub eventHub, IAudioEditorService audioEditorService, IAudioRepository audioRepository, IEnumerable<IDataGridService> service)
        {
            _services = service.ToDictionary(service => (service.DataGrid, service.NodeType));
        }

        public IDataGridService GetService(AudioProjectDataGrid dataGrid, NodeType nodeType)
        {
            if (!_services.TryGetValue((dataGrid, nodeType), out var service))
                throw new InvalidOperationException($"No service defined for node type {nodeType}.");
            return service;
        }
    }
}
