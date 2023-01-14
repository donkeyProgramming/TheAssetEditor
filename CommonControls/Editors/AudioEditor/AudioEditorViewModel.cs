using CommonControls.Common;
using CommonControls.Editors.Sound;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonControls.Editors.AudioEditor
{

    public class SelectedHircItem
    {
        public string DisplayName { get; set; }
        public uint Id { get; set; }
        public string PackFile { get; set; }
        public uint IndexInFile { get; set; }
        public HircItem HircItem { get; set; }
    }


    public class EventSelectionFilter
    {
        private readonly WWiseNameLookUpHelper _lookUpHelper;
        private readonly ExtenededSoundDataBase _globalDb;

        public FilterCollection<SelectedHircItem> EventList { get; set; }

        public EventSelectionFilter(WWiseNameLookUpHelper lookUpHelper, ExtenededSoundDataBase globalDb)
        {
            _lookUpHelper = lookUpHelper;
            _globalDb = globalDb;

            EventList = new FilterCollection<SelectedHircItem>(new List<SelectedHircItem>())
            {
                SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; }
            };

            Refresh(true, true);
        }

        public void Refresh(bool showEvents, bool showDialogEvents)
        {
            var typesToShow = new List<HircType>();
            if (showEvents)
                typesToShow.Add(HircType.Event);
            if (showDialogEvents)
                typesToShow.Add(HircType.Dialogue_Event);

            var allEvents = _globalDb.HircList.SelectMany(x => x.Value)
                .Where(x => typesToShow.Contains(x.Type))
                .ToList();

            var selectedableList = allEvents.Select(x => new SelectedHircItem() { HircItem = x, DisplayName = _lookUpHelper.GetName(x.Id), Id = x.Id, PackFile = x.OwnerFile, IndexInFile = x.IndexInFile }).ToList();
            EventList.Filter = "";
            EventList.UpdatePossibleValues(selectedableList);
        }
    }

    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public EventSelectionFilter EventFilter { get; set; }

        private readonly PackFileService _pfs;
        PackFile _mainFile;
        List<AudioTreeNode> _treNodeList = new List<AudioTreeNode>();
        HircTreeItem _selectedNode;

        // Public attributes
        public ObservableCollection<HircTreeItem> TreeList { get; set; } = new ObservableCollection<HircTreeItem>();
        public HircTreeItem SelectedNode { get => _selectedNode; set { SetAndNotify(ref _selectedNode, value); NodeSelected(_selectedNode); } }

        public NotifyAttr<bool> ShowIds { get; set; }
        public NotifyAttr<bool> ShowBnkName { get; set; }
        public NotifyAttr<bool> UseBnkNameWhileParsing { get; set; }
        public NotifyAttr<bool> ShowEvents { get; set; }
        public NotifyAttr<bool> ShowDialogEvents { get; set; }


        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public NotifyAttr<string> SelectedNodeText { get; set; } = new NotifyAttr<string>("");
        public PackFile MainFile { get => _mainFile; set { _mainFile = value; Load(_mainFile); } }
        public bool HasUnsavedChanges { get; set; }

        WWiseNameLookUpHelper _lookUpHelper;
        ExtenededSoundDataBase _globalDb;
        SoundPlayer _player;

        public AudioEditorViewModel(PackFileService pfs)
        {
            _pfs = pfs;
            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            _globalDb = builder.BuildMasterSoundDatabase(bnkList);
            _lookUpHelper = builder.BuildNameHelper(pfs);

            _player = new SoundPlayer(pfs, _lookUpHelper);
            ShowIds = new NotifyAttr<bool>(false, RefeshList);
            ShowBnkName = new NotifyAttr<bool>(false, RefeshList);
            UseBnkNameWhileParsing = new NotifyAttr<bool>(false, RefeshList);
            ShowEvents = new NotifyAttr<bool>(true, RefeshList);
            ShowDialogEvents = new NotifyAttr<bool>(true, RefeshList);

            EventFilter = new EventSelectionFilter(_lookUpHelper, _globalDb);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;
            RefeshList(true);
        }

        void RefeshList(bool newValue) => EventFilter.Refresh(ShowEvents.Value, ShowDialogEvents.Value);


        private void OnEventSelected(SelectedHircItem newValue)
        {
            if (newValue?.Id == _selectedNode?.Item?.Id)
                return;

            if (newValue != null)
            {
                _selectedNode = null;
                TreeList.Clear();

                var parser = new WWiseTreeParserChildren(_globalDb, _lookUpHelper, ShowIds.Value, ShowBnkName.Value, UseBnkNameWhileParsing.Value);
                var rootNode = parser.BuildHierarchy(newValue.HircItem);
                TreeList.Add(rootNode);
            }
        }

        private void Load(PackFile mainFile) { }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;
        }

        public bool Save()
        {
            return true;
        }

        void NodeSelected(HircTreeItem selectedNode)
        {
            if (selectedNode != null)
            {
                var hircAsString = JsonSerializer.Serialize((object)selectedNode.Item, new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter() }, WriteIndented = true });
                SelectedNodeText.Value = hircAsString;

                var parser = new WWiseTreeParserParent(_globalDb, _lookUpHelper, true, true, true);
                var rootNode = parser.BuildHierarchy(selectedNode.Item);

                var flatList = GetListstuff(rootNode);
                flatList.Reverse();


                SelectedNodeText.Value += "\n\nParent structure:\n";
                foreach (var str in flatList)
                    SelectedNodeText.Value += "\t" + str + "\n";
            }
            else
            {
                SelectedNodeText.Value = "";
            }
        }

        List<string> GetListstuff(HircTreeItem root)
        {
            var childData = new List<string>();
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                    childData.AddRange(GetListstuff(child));
            }

            childData.Add(root.DisplayName);
            return childData;
        }

        public void PlaySelectedSoundAction()
        {
            _player.PlaySound(TreeList.FirstOrDefault(), _selectedNode.Item as ICAkSound);
        }


        class OutputTest
        {
            public string EventName;
            public List<string> keys = new List<string>();
            public List<string[]> Table;

            public List<string> PrettyTable;
            public string PrettyKeys;
        }

        public void ExportIdListAction()
        {
            var dialogs = _globalDb.HircList.SelectMany(x => x.Value).Where(x => x.Type == HircType.Dialogue_Event).Cast<FileTypes.Sound.WWise.Hirc.V136.CAkDialogueEvent_v136>().ToList();

            var whereRootNotZero = dialogs.Where(x => x.AkDecisionTree.Root.Key != 0).ToList();
            var whereFirstNotZero = dialogs.Where(x => x.AkDecisionTree.Root.Children.First().Key != 0).ToList();
            var counts = dialogs.Select(x => x.ArgumentList.Arguments.Count()).Distinct().ToList();

            List<OutputTest> test = new List<OutputTest>();

            foreach (var dialog in dialogs)
            {
                var numArgs = dialog.ArgumentList.Arguments.Count()-1;
                var root = dialog.AkDecisionTree.Root.Children.First();

                if (numArgs != 0)
                {
                    var rowIndex = 0;
                    var table = new List<string>();
                    foreach (var children in root.Children)
                        GenerateRow(children, 0, numArgs, new Stack<string>(), table);

                    var keys = dialog.ArgumentList.Arguments.Select(x => _lookUpHelper.GetName(x.ulGroup)).ToList();
                    test.Add(new OutputTest()
                    {
                        EventName = _lookUpHelper.GetName(dialog.Id),
                        keys = keys,
                        //Table = table,

                        PrettyKeys = string.Join("|", keys),
                        PrettyTable = table.Select(x=> string.Join("|", x)).ToList()
                    });

                    var last = test.Last();

                    var wholeStr = new StringBuilder();
                    wholeStr.AppendLine("sep=|");
                    wholeStr.AppendLine(last.PrettyKeys);
                    foreach (var row in last.PrettyTable)
                        wholeStr.AppendLine(row);
                    DirectoryHelper.EnsureCreated("c:\\temp\\wwiseDialogEvents");
                    System.IO.File.WriteAllText($"c:\\temp\\wwiseDialogEvents\\{last.EventName}.csv", wholeStr.ToString());

                }

            }

            // Remove root, remove first
           //What is ""Node 0"" as first node // first audio node. ALways only 1?
           //
           //var cool = test.Where(x => x.EventName.Contains("vo_order_Attack", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            //var args = dialogs.Cast<FileTypes.Sound.WWise.Hirc.V136.CAkDialogueEvent_v136>().Select(x => x.ArgumentList).SelectMany(x=>x.Arguments).ToList();
            //
            //var groupTypes = args.Select(x => x.eGroupType).Distinct().OrderByDescending(x=>x).ToList();
            //var argTypes = args.GroupBy(x => x.eGroupType).OrderBy(x => x.Key).ToList();
            //
            //var items2 = argTypes.Select(x => $"{x.Key}: { string.Join(",", x.Select(y => y.ulGroup).Distinct())}").ToList();
            //var test = string.Join("\n", items2);

            _lookUpHelper.SaveToFileWithId("c:\\temp\\wwiseIds.txt");
        }

        void GenerateRow(FileTypes.Sound.WWise.Hirc.V136.AkDecisionTree.Node currentNode, int currentArgrument, int numArguments, Stack<string> pushList, List<string> outputList)
        {
            var currentNodeContent = _lookUpHelper.GetName(currentNode.Key);
            pushList.Push(currentNodeContent);

            bool isDone = numArguments == currentArgrument;
            if (isDone)
            {
                outputList.Add(string.Join("|", pushList.ToArray().Reverse()));
            }
            else
            {
                foreach (var child in currentNode.Children)
                    GenerateRow(child, currentArgrument + 1, numArguments, pushList, outputList);
            }
        }
    }
}
