// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using CommonControls.Editors.TextEditor;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.Editors.TextEditor;

namespace Shared.Ui.Editors.Wtui
{
    public class TwUiViewModel : NotifyPropertyChangedImpl, IEditorViewModel, ITextEditorViewModel
    {
        PackFile _mainFile;
        UiTreeNode _selectedNode;

        // Public attributes
        public ObservableCollection<UiTreeNode> TreeList { get; set; } = new ObservableCollection<UiTreeNode>();
        public UiTreeNode SelectedNode { get => _selectedNode; set { SetAndNotify(ref _selectedNode, value); NodeSelected(_selectedNode); } }

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>();
        public PackFile MainFile { get => _mainFile; set { _mainFile = value; Load(_mainFile); } }
        public bool HasUnsavedChanges { get; set; }

        public ITextEditor TextEditor { get; private set; }
        string _text;
        public string Text
        {
            get => _text;
            set
            {
                SetAndNotify(ref _text, value);
                WriteTextToNode(value, SelectedNode);
            }
        }

        public TwUiViewModel()
        {
        }

        private void Load(PackFile mainFile)
        {
            DisplayName.Value = mainFile.Name;
            var bytes = mainFile.DataSource.ReadData();
            var xmlText = System.Text.Encoding.UTF8.GetString(bytes);
            var componentList = TwUiParser.LoadAllComponents(xmlText);
            var rootNote = TwUiParser.GenerateLayoutTree(xmlText, componentList);
            TreeList.Add(rootNote);
        }

        public void Close()
        {
        }

        public bool Save()
        {
            return true;
        }

        void NodeSelected(UiTreeNode selectedNode)
        {
            if (selectedNode == null)
                Text = "";
            else
                Text = selectedNode.XmlContent;
        }

        void WriteTextToNode(string text, UiTreeNode node)
        {
            if (node != null)
                node.XmlContent = text;
        }


        public void SetEditor(ITextEditor theEditor)
        {
            TextEditor = theEditor;
            TextEditor.ClearUndoStack();
        }
    }
}
