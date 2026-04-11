using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Shared.Core.Events;
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

    public class ContextMenuFactory
    {
        private readonly IEnumerable<IContextMenuBuilder> _builders;

        public ContextMenuFactory(IEnumerable<IContextMenuBuilder> builders)
        {
            _builders = builders;
        }

        public IContextMenuBuilder GetContextMenu(ContextMenuType menuType) => _builders.First(x => x.Type == menuType);
    }

    public interface IContextMenuBuilder
    {
        ContextMenuType Type { get; }
        public ObservableCollection<ContextMenuItem2> Build(TreeNode? node);
    }

    public abstract class ContextMenuBuilder : IContextMenuBuilder
    {
        private readonly IUiCommandFactory _commandFactory;

        public ContextMenuType Type { get; private set; }

        public ContextMenuBuilder(ContextMenuType type, IUiCommandFactory commandFactory)
        {
            Type = type;
            _commandFactory = commandFactory;
        }

        protected abstract void Create(ContextMenuItem2 rootNode, TreeNode selectedNode);

        public ObservableCollection<ContextMenuItem2> Build(TreeNode? node)
        {
            var output = new ObservableCollection<ContextMenuItem2>();
            if (node == null)
                return output;

            var placeholderRoot = new ContextMenuItem2("Root", null);
            Create(placeholderRoot, node);

            foreach (var item in placeholderRoot.ContextMenu)
                output.Add(item);

            return output;
        }


        protected void Add<T>(TreeNode? node, ContextMenuItem2 parent) where T : IContextMenuCommand
        {
            var instance = _commandFactory.Create<T>();

            if (instance.IsEnabled(node) == false)
                return;

            var name = instance.GetDisplayName(node);
            var item = new ContextMenuItem2(name, () => instance.Execute(node));
            parent.ContextMenu.Add(item);
        }

        protected void AddSeperator(ContextMenuItem2 parent)
        {
            parent.ContextMenu.Add(null);
        }

        public ContextMenuItem2 AddChildMenu(string name, ContextMenuItem2 parent)
        {
            var newItem = new ContextMenuItem2(name, null);
            parent.ContextMenu.Add(newItem);
            return newItem;

        }
    }
}
