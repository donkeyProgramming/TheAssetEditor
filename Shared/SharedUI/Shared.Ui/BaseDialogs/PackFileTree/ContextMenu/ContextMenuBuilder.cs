using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Shared.Ui.BaseDialogs.PackFileTree.ContextMenu
{
    public class PackFileContextMenuComposer
    {
        private readonly PackFileContextMenuRegistry _registry;
        private readonly IServiceProvider _serviceProvider;

        public PackFileContextMenuComposer(PackFileContextMenuRegistry registry, IServiceProvider serviceProvider)
        {
            _registry = registry;
            _serviceProvider = serviceProvider;
        }

        public ObservableCollection<ContextMenuItem> Build(ContextMenuType contextMenuType, TreeNode? node)
        {
            var output = new ObservableCollection<ContextMenuItem>();
            if (node == null)
                return output;

            var items = _registry.Get(contextMenuType);
            var hasClusterContent = false;

            foreach (var cluster in System.Enum.GetValues<ContextMenuCluster>())
            {
                var clusterRoot = new ContextMenuItem("Root", null);
                var pathToMenuLookup = new Dictionary<string, ContextMenuItem>(System.StringComparer.OrdinalIgnoreCase);

                foreach (var item in items.Where(x => x.Cluster == cluster))
                {
                    var command = (IContextMenuCommand)_serviceProvider.GetRequiredService(item.CommandType);
                    if (!command.ShouldAdd(node) || !command.IsEnabled(node))
                        continue;

                    var parent = GetOrCreateMenuPath(item.Path, clusterRoot, pathToMenuLookup);
                    var x = new ContextMenuItem(command.GetDisplayName(node), () => command.Execute(node));
                    parent.ContextMenu.Add(x);
                }

                RemoveEmptySubmenus(clusterRoot);
                if (clusterRoot.ContextMenu.Count == 0)
                    continue;

                if (hasClusterContent)
                    output.Add(null);

                foreach (var menuItem in clusterRoot.ContextMenu)
                    output.Add(menuItem);

                hasClusterContent = true;
            }

            return output;
        }



        private static ContextMenuItem GetOrCreateMenuPath(string path, ContextMenuItem root, Dictionary<string, ContextMenuItem> pathToMenuLookup)
        {
            if (string.IsNullOrWhiteSpace(path))
                return root;

            var current = root;
            var runningPath = string.Empty;
            foreach (var pathSegment in path.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                runningPath = string.IsNullOrEmpty(runningPath) ? pathSegment : $"{runningPath}/{pathSegment}";

                if (pathToMenuLookup.TryGetValue(runningPath, out var menu) == false)
                {
                    menu = new ContextMenuItem(pathSegment, null);
                    current.ContextMenu.Add(menu);
                    pathToMenuLookup[runningPath] = menu;
                }

                current = menu;
            }

            return current;
        }

        private static void RemoveEmptySubmenus(ContextMenuItem root)
        {
            for (var i = root.ContextMenu.Count - 1; i >= 0; i--)
            {
                var menuItem = root.ContextMenu[i];
                if (menuItem == null)
                    continue;

                if (menuItem.ContextMenu.Count > 0)
                    RemoveEmptySubmenus(menuItem);

                if (menuItem.Command == null && menuItem.ContextMenu.Count == 0)
                    root.ContextMenu.RemoveAt(i);
            }
        }
    }
}
