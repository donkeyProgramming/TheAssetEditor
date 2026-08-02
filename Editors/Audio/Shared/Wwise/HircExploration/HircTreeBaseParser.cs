using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public abstract class HircTreeBaseParser(IAudioRepository audioRepository)
    {
        private sealed record PendingHircNode(uint HircId, HircTreeNode Parent);

        public readonly IAudioRepository AudioRepository = audioRepository;
        public readonly Dictionary<AkBkHircType, Action<HircItem, HircTreeNode>> HircProcessChildMap = [];

        private readonly List<PendingHircNode> _breadthFirstSearchFrontier = [];

        public HircTreeNode BuildHierarchy(HircItem item)
        {
            var root = new HircTreeNode();
            ProcessHircObject(item, root);
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
            if (HircProcessChildMap.TryGetValue(item.HircType, out var func))
                func(item, parent);
            else
            {
                var unknownNode = new HircTreeNode() { DisplayName = $"Unknown node type {item.HircType} for ID {item.Id} in {item.BnkFilePath}", Hirc = item };
                parent.Children.Add(unknownNode);
            }
        }

        protected void ProcessNext(uint hircId, HircTreeNode parent)
        {
            if (hircId == 0)
                return;

            _breadthFirstSearchFrontier.Add(new PendingHircNode(hircId, parent));
        }

        protected void ProcessNext(List<uint> ids, HircTreeNode parent)
        {
            foreach (var id in ids)
                ProcessNext(id, parent);
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
