using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public abstract class HircTreeBaseParser(IAudioRepository audioRepository)
    {
        public readonly IAudioRepository AudioRepository = audioRepository;
        public readonly Dictionary<AkBkHircType, Action<HircItem, HircTreeNode>> HircProcessChildMap = [];

        public HircTreeNode BuildHierarchy(HircItem item)
        {
            var root = new HircTreeNode();
            ProcessHircObject(item, root);
            var actualRoot = root.Children.FirstOrDefault();
            actualRoot.Parent = null;
            root.Children = null;
            return actualRoot;
        }

        public List<HircTreeNode> BuildHierarchyAsFlatList(HircItem item)
        {
            var rootNode = BuildHierarchy(item);
            var flatList = GetHircParents(rootNode);
            return flatList;
        }

        private static List<HircTreeNode> GetHircParents(HircTreeNode root)
        {
            var childData = new List<HircTreeNode>();
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                    childData.AddRange(GetHircParents(child));
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
                var unknownNode = new HircTreeNode() { DisplayName = $"Unknown node type {item.HircType} for ID {item.Id} in {item.BnkFilePath}", Item = item };
                parent.Children.Add(unknownNode);
            }
        }

        protected void ProcessNext(uint hircId, HircTreeNode parent)
        {
            if (hircId == 0)
                return;

            var hircs = AudioRepository.GetHircs(hircId);
            var hirc = hircs.FirstOrDefault();
            if (hirc == null)
                parent.Children.Add(new HircTreeNode() { DisplayName = $"Error: Unable to find Hirc with ID {hircId}" });
            else
                ProcessHircObject(hirc, parent);
        }

        protected void ProcessNext(List<uint> ids, HircTreeNode parent)
        {
            foreach (var id in ids)
                ProcessNext(id, parent);
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
                throw new Exception($"HircItem with Id {instance.Id} is of type {instance.GetType().Name} and cannot be converted to {typeof(Wanted).Name}.");
            return wanted;
        }
    }
}
