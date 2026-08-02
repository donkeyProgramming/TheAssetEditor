using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public abstract class HircTreeBaseParser(IAudioRepository audioRepository, bool lazyLoadChildren = false)
    {
        private sealed record PendingHircNode(uint HircId, HircTreeNode Parent);

        public readonly IAudioRepository AudioRepository = audioRepository;
        public readonly Dictionary<AkBkHircType, Action<HircItem, HircTreeNode>> HircProcessChildMap = [];

        private readonly List<PendingHircNode> _breadthFirstSearchFrontier = [];
        protected bool LazyLoadChildren { get; } = lazyLoadChildren;

        public HircTreeNode BuildHierarchy(HircItem item)
        {
            var root = new HircTreeNode();
            ProcessHircObject(item, root);
            if (!LazyLoadChildren)
                RunBreadthFirstSearch();

            var actualRoot = root.Children.FirstOrDefault();
            actualRoot.Parent = null;
            root.Children = null;
            return actualRoot;
        }

        public List<HircTreeNode> BuildHierarchyAsFlatList(HircItem item)
        {
            var rootNode = BuildHierarchy(item);
            var flatList = RunDepthFirstSearch(rootNode);
            return flatList;
        }

        private static List<HircTreeNode> RunDepthFirstSearch(HircTreeNode root)
        {
            var childData = new List<HircTreeNode>();
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                    childData.AddRange(RunDepthFirstSearch(child));
            }

            childData.Add(root);
            return childData;
        }

        private void ProcessHircObject(HircItem item, HircTreeNode parent)
        {
            var firstNewChildIndex = parent.Children.Count;

            if (HircProcessChildMap.TryGetValue(item.HircType, out var func))
                func(item, parent);
            else
            {
                var unknownNode = new HircTreeNode() { DisplayName = $"Unknown node type {item.HircType} for ID {item.Id} in {item.BnkFilePath}", Hirc = item };
                parent.Children.Add(unknownNode);
            }

            for (var i = firstNewChildIndex; i < parent.Children.Count; i++)
                SetParentLinks(parent.Children[i], parent);
        }

        private static void SetParentLinks(HircTreeNode node, HircTreeNode parent)
        {
            var pendingNodes = new Stack<(HircTreeNode Node, HircTreeNode Parent)>();
            var visitedNodes = new HashSet<HircTreeNode>();
            pendingNodes.Push((node, parent));

            while (pendingNodes.Count != 0)
            {
                var current = pendingNodes.Pop();
                if (!visitedNodes.Add(current.Node))
                    continue;

                current.Node.Parent = current.Parent;
                if (current.Node.Children == null)
                    continue;

                foreach (var child in current.Node.Children)
                    pendingNodes.Push((child, current.Node));
            }
        }

        protected void ProcessNext(uint hircId, HircTreeNode parent)
        {
            if (hircId == 0)
                return;

            if (!LazyLoadChildren)
            {
                _breadthFirstSearchFrontier.Add(new PendingHircNode(hircId, parent));
                return;
            }

            parent.PendingChildHircIds.Add(hircId);
            if (parent.PendingChildHircIds.Count == 1)
            {
                parent.Children.Add(new HircTreeNode() { DisplayName = "Loading..." });
                parent.ResolveChildrenCallback = ResolveChildren;
            }
        }

        protected void ProcessNext(List<uint> ids, HircTreeNode parent)
        {
            foreach (var id in ids)
                ProcessNext(id, parent);
        }

        private void ResolveChildren(HircTreeNode node)
        {
            var pendingIds = node.PendingChildHircIds;

            var hircIds = pendingIds.Distinct().ToList();
            var hircsById = AudioRepository.GetHircs(hircIds);

            node.Children.Clear();

            foreach (var hircId in pendingIds)
            {
                if (IsHircInAncestry(node, hircId))
                    node.Children.Add(new HircTreeNode() { DisplayName = $"Circular HIRC reference to ID {hircId}" });
                else if (hircsById.TryGetValue(hircId, out var hircs) && hircs.Count != 0)
                    ProcessHircObject(hircs[0], node);
                else
                    node.Children.Add(new HircTreeNode() { DisplayName = $"Error: Unable to find Hirc with ID {hircId}" });
            }

            pendingIds.Clear();
        }

        private static bool IsHircInAncestry(HircTreeNode node, uint hircId)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current.Hirc?.Id == hircId)
                    return true;
            }

            return false;
        }

        private void RunBreadthFirstSearch()
        {
            while (_breadthFirstSearchFrontier.Count != 0)
            {
                var currentDepth = new List<PendingHircNode>(_breadthFirstSearchFrontier);
                _breadthFirstSearchFrontier.Clear();

                var hircIdsAtDepth = currentDepth.Select(pendingNode => pendingNode.HircId).Distinct().ToList();
                var hircsById = AudioRepository.GetHircs(hircIdsAtDepth);

                foreach (var pendingNode in currentDepth)
                {
                    if (hircsById.TryGetValue(pendingNode.HircId, out var hircs) && hircs.Count != 0)
                        ProcessHircObject(hircs[0], pendingNode.Parent);
                    else
                        pendingNode.Parent.Children.Add(new HircTreeNode() { DisplayName = $"Error: Unable to find Hirc with ID {pendingNode.HircId}" });
                }
            }
        }

        protected virtual string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = AudioRepository.GetNameFromId(id, out var found);
            if (hidenNameIfMissing)
                name = "";
            return name;
        }

        protected static Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            if (instance is not Wanted wanted)
                throw new Exception($"Hirc with Id {instance.Id} is of type {instance.GetType().Name} and cannot be converted to {typeof(Wanted).Name}.");
            return wanted;
        }
    }
}
