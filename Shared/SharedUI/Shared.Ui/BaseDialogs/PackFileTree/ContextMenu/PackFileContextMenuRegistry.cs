using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu
{
    public enum ContextMenuType
    {
        None,
        MainApplication,
        Simple,
        SceneExplorer
    }

    public enum ContextMenuCluster
    {
        PackFileOperation,
        FolderOperation,
        FileOperation,
        Export,
        Reports,
        Misc
    }

    public interface IPackFileContextMenuRegistration
    {
        void Register(PackFileContextMenuRegistry registry);
    }

    public sealed class PackFileContextMenuItemRegistration
    {
        public required Type CommandType { get; init; }
        public required ContextMenuType ContextMenuType { get; init; }
        public required ContextMenuCluster Cluster { get; init; }
        public required string Path { get; init; }
        public required int Priority { get; init; }
    }

    public class PackFileContextMenuRegistry
    {
        private readonly List<PackFileContextMenuItemRegistration> _items = [];

        public void RegisterPackFileContextMenuItem<TCommand>(ContextMenuType contextMenuType, string path, int priority, ContextMenuCluster cluster)
            where TCommand : IContextMenuCommand
        {
            RegisterPackFileContextMenuItem(typeof(TCommand), contextMenuType, path, priority, cluster);
        }

        public void RegisterPackFileContextMenuItem(Type commandType, ContextMenuType contextMenuType, string path, int priority, ContextMenuCluster cluster)
        {
            if (!typeof(IContextMenuCommand).IsAssignableFrom(commandType))
                throw new ArgumentException($"{commandType.Name} must implement {nameof(IContextMenuCommand)}");

            _items.Add(new PackFileContextMenuItemRegistration
            {
                CommandType = commandType,
                ContextMenuType = contextMenuType,
                Cluster = cluster,
                Path = path ?? string.Empty,
                Priority = priority
            });
        }

        public IReadOnlyList<PackFileContextMenuItemRegistration> Get(ContextMenuType contextMenuType)
        {
            return _items
                .Where(x => x.ContextMenuType == contextMenuType)
                .OrderBy(x => x.Cluster)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.Path)
                .ThenBy(x => x.CommandType.FullName)
                .ToList();
        }
    }
}
