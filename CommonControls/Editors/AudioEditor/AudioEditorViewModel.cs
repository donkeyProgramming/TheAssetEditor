using CommonControls.Common;
using CommonControls.Editors.Sound;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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

            var allEvents = _globalDb.HircList.SelectMany(x => x.Value)
                .Where(x => x.Type == HircType.Event)
                .ToList();

            var selectedableList = allEvents.Select(x => new SelectedHircItem() { HircItem = x, DisplayName = _lookUpHelper.GetName(x.Id), Id = x.Id, PackFile = x.OwnerFile, IndexInFile = x.IndexInFile }).ToList();

            EventList = new FilterCollection<SelectedHircItem>(selectedableList)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; }
            };
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

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public PackFile MainFile { get => _mainFile; set { _mainFile = value; Load(_mainFile); } }
        public bool HasUnsavedChanges { get; set; }

        WWiseNameLookUpHelper _lookUpHelper;
        ExtenededSoundDataBase _globalDb;

        public AudioEditorViewModel(PackFileService pfs)
        {
            _pfs = pfs;
            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            _globalDb = builder.BuildMasterSoundDatabase(bnkList);
            _lookUpHelper = builder.BuildNameHelper(pfs);


            EventFilter = new EventSelectionFilter(_lookUpHelper, _globalDb);
            EventFilter.EventList.SelectedItemChanged += EventList_SelectedItemChanged;
        }

        private void EventList_SelectedItemChanged(SelectedHircItem newValue)
        {
            if (newValue?.Id == _selectedNode?.Item.Id)
                return;

            //TreeList.Clear();
            //_selectedNode = null;
            if (newValue != null)
            {
                _selectedNode = null;
                TreeList.Clear();

                WWiseTreeParser parser = new WWiseTreeParser(_globalDb, _lookUpHelper);
                var rootNode = parser.BuildEventHierarchy(newValue.HircItem);
                TreeList.Add(rootNode);

            }
        }

        private void Load(PackFile mainFile) { }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= EventList_SelectedItemChanged;
        }

        public bool Save()
        {
            return true;
        }

        void NodeSelected(HircTreeItem selectedNode)
        {
            //if (selectedNode == null)
            //    Text = "";
            //else
            //    Text = selectedNode.XmlContent;
        }
    }
}
