using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.AudioProjectViewer.Table;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.Table
{
    public interface IViewerTableServiceFactory
    {
        IViewerTableService GetService(AudioProjectTreeNodeType nodeType);
    }

    public class ViewerTableServiceFactory : IViewerTableServiceFactory
    {
        private readonly Dictionary<AudioProjectTreeNodeType, IViewerTableService> _services;

        public ViewerTableServiceFactory(IEnumerable<IViewerTableService> services)
        {
            _services = services.ToDictionary(service => service.NodeType);
        }

        public IViewerTableService GetService(AudioProjectTreeNodeType nodeType)
        {
            if (!_services.TryGetValue(nodeType, out var service))
                throw new InvalidOperationException($"No service defined for node type {nodeType}.");
            return service;
        }
    }
}
