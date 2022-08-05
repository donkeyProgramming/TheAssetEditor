using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public class EventFilter
    {
        private readonly SoundDatFile _soundDatDb;

        public FilterCollection<string> EventList { get; set; }

        public EventFilter(SoundDatFile soundDatDb)
        {
            _soundDatDb = soundDatDb;
            EventList = new FilterCollection<string>(null)
            {
                SearchFilter = (value, rx) => { return rx.Match(value).Success; }
            };
        }
    }

    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        EventFilter EventFilter { get; set; }



        private readonly PackFileService _pfs;
        PackFile _mainFile;
        List<AudioTreeNode> _treNodeList = new List<AudioTreeNode>();
        AudioTreeNode _selectedNode;

        // Public attributes
        public ObservableCollection<AudioTreeNode> TreeList { get; set; } = new ObservableCollection<AudioTreeNode>();
        public AudioTreeNode SelectedNode { get => _selectedNode; set { SetAndNotify(ref _selectedNode, value); NodeSelected(_selectedNode); } }

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public PackFile MainFile { get => _mainFile; set { _mainFile = value; Load(_mainFile); } }
        public bool HasUnsavedChanges { get; set; }

      

        public AudioEditorViewModel(PackFileService pfs)
        {
            _pfs = pfs;
        }

        private void Load(PackFile mainFile) { }

        public void Close()
        {
        }

        public bool Save()
        {
            return true;
        }

        void NodeSelected(AudioTreeNode selectedNode)
        {
           //if (selectedNode == null)
           //    Text = "";
           //else
           //    Text = selectedNode.XmlContent;
        }
    }
}
