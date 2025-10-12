using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioExplorer;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Utility
{
    public abstract class WwiseTreeParserBase
    {
        public readonly Dictionary<AkBkHircType, Action<HircItem, HircTreeItem>> _hircProcessChildMap = [];
        public readonly IAudioRepository _audioRepository;

        public WwiseTreeParserBase(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public HircTreeItem BuildHierarchy(HircItem item)
        {
            var root = new HircTreeItem();
            ProcessHircObject(item, root);
            var actualRoot = root.Children.FirstOrDefault();
            actualRoot.Parent = null;
            root.Children = null;
            return actualRoot;
        }

        public List<HircTreeItem> BuildHierarchyAsFlatList(HircItem item)
        {
            var rootNode = BuildHierarchy(item);

            var flatList = GetHircParents(rootNode);
            return flatList;
        }

        private static List<HircTreeItem> GetHircParents(HircTreeItem root)
        {
            var childData = new List<HircTreeItem>();
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                    childData.AddRange(GetHircParents(child));
            }

            childData.Add(root);
            return childData;
        }

        private void ProcessHircObject(HircItem item, HircTreeItem parent)
        {
            if (_hircProcessChildMap.TryGetValue(item.HircType, out var func))
                func(item, parent);
            else
            {
                var unknownNode = new HircTreeItem() { DisplayName = $"Unknown node type {item.HircType} for Id {item.Id} in {item.BnkFilePath}", Item = item };
                parent.Children.Add(unknownNode);
            }
        }

        protected void ProcessNext(uint hircId, HircTreeItem parent)
        {
            if (hircId == 0)
                return;

            var instances = _audioRepository.GetHircs(hircId);
            var hircItem = instances.FirstOrDefault();
            if (hircItem == null)
                parent.Children.Add(new HircTreeItem() { DisplayName = $"Error: Unable to find Id {hircId}" });
            else
                ProcessHircObject(hircItem, parent);
        }


        protected void ProcessNext(List<uint> ids, HircTreeItem parent)
        {
            foreach (var id in ids)
                ProcessNext(id, parent);
        }

        protected virtual string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = _audioRepository.GetNameFromId(id, out var found);
            if (hidenNameIfMissing)
                name = "";
            return name;
        }

        protected string GetDisplayId(HircItem item, bool hidenNameIfMissing) => GetDisplayId(item.Id, item.BnkFilePath, hidenNameIfMissing);

        protected static Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            if (instance is not Wanted wanted)
                throw new Exception($"HircItem with Id {instance.Id} is of type {instance.GetType().Name} and cannot be converted to {typeof(Wanted).Name}.");
            return wanted;
        }
    }
}
