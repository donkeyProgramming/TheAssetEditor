using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectEditor.Table
{
    public interface IEditorTableServiceFactory
    {
        IEditorTableService GetService(AudioProjectTreeNodeType nodeType);
    }

    public class EditorTableServiceFactory : IEditorTableServiceFactory
    {
        private readonly Dictionary<AudioProjectTreeNodeType, IEditorTableService> _services;

        public EditorTableServiceFactory(IEnumerable<IEditorTableService> services)
        {
            _services = services.ToDictionary(service => service.NodeType);
        }

        public IEditorTableService GetService(AudioProjectTreeNodeType nodeType)
        {
            if (!_services.TryGetValue(nodeType, out var service))
                throw new InvalidOperationException($"No service defined for node type {nodeType}.");
            return service;
        }
    }
}
