using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.Table
{
    public interface IEditorTableServiceFactory
    {
        IEditorTableService GetService(AudioProjectExplorerTreeNodeType nodeType);
    }

    public class EditorTableServiceFactory : IEditorTableServiceFactory
    {
        private readonly Dictionary<AudioProjectExplorerTreeNodeType, IEditorTableService> _services;

        public EditorTableServiceFactory(IEnumerable<IEditorTableService> services)
        {
            _services = services.ToDictionary(service => service.NodeType);
        }

        public IEditorTableService GetService(AudioProjectExplorerTreeNodeType nodeType)
        {
            if (!_services.TryGetValue(nodeType, out var service))
                throw new InvalidOperationException($"No service defined for node type {nodeType}.");
            return service;
        }
    }
}
