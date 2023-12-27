using Audio.FileFormats.WWise;
using Audio.Storage;
using CommonControls.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audio.AudioEditor
{
    public abstract class WWiseTreeParserBase
    {
        protected ILogger _logger = Logging.Create<WWiseTreeParserBase>();

        protected Dictionary<HircType, Action<HircItem, HircTreeItem>> _hircProcessChildMap = new Dictionary<HircType, Action<HircItem, HircTreeItem>>();
        protected readonly IAudioRepository _repository;

        protected readonly bool _showId;
        protected readonly bool _showOwningBnkFile;
        protected readonly bool _filterByBnkName;

        public WWiseTreeParserBase(IAudioRepository repository, bool showId, bool showOwningBnkFile, bool filterByBnkName)
        {
            _repository = repository;
            _showId = showId;
            _showOwningBnkFile = showOwningBnkFile;
            _filterByBnkName = filterByBnkName;
        }

        public HircTreeItem BuildHierarchy(HircItem item)
        {
            HircTreeItem root = new HircTreeItem();
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
            //flatList.Reverse();
            return flatList;
        }

        List<HircTreeItem> GetHircParents(HircTreeItem root)
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


        void ProcessHircObject(HircItem item, HircTreeItem parent)
        {
            if (_hircProcessChildMap.TryGetValue(item.Type, out var func))
            {
                func(item, parent);
            }
            else
            {
                var unknownNode = new HircTreeItem() { DisplayName = $"Unknown node type {item.Type} for Id {item.Id} in {item.OwnerFile}", Item = item };
                parent.Children.Add(unknownNode);
            }
        }


        protected void ProcessNext(uint hircId, HircTreeItem parent)
        {
            if (hircId == 0)
                return;

            var instances = _repository.GetHircObject(hircId);
            var hircItem = instances.FirstOrDefault();
            if (hircItem == null)
                parent.Children.Add(new HircTreeItem() { DisplayName = $"Error: Unable to find ID {hircId}" });
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
            var name = _repository.GetNameFromHash(id, out var found);
            if (hidenNameIfMissing)
                name = "";
            if (found == true && _showId)
                name += " " + id;
            if (_showOwningBnkFile && string.IsNullOrWhiteSpace(fileName) == false)
                name += " " + fileName;
            return name;
        }

        protected string GetDisplayId(HircItem item, bool hidenNameIfMissing) => GetDisplayId(item.Id, item.OwnerFile, hidenNameIfMissing);

        protected Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            var wanted = instance as Wanted;
            if (wanted == null)
                throw new Exception();
            return wanted;
        }
    }
}
