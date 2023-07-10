// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommonControls.Common;
using CommonControls.Editors.AnimMeta.View;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;

namespace CommonControls.Editors.AnimMeta
{
    public class EditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public event EditorSavedDelegate EditorSavedEvent;

        ILogger _logger = Logging.Create<EditorViewModel>();

        PackFileService _pf;
        CopyPasteManager _copyPasteManager;
        MetaDataFile _metaDataFile;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>();

        PackFile _file;
        public PackFile MainFile { get => _file; set => Initialise(value); }


        public ObservableCollection<MetaTagViewBase> Tags { get; set; } = new ObservableCollection<MetaTagViewBase>();

        MetaTagViewBase _selectedTag;
        MetaTagViewBase _prevSelectedTag;
        List<MetaTagViewBase> _selectedTags = new List<MetaTagViewBase>();
        public MetaTagViewBase SelectedTag { get => _selectedTag; set => SetAndNotify(ref _selectedTag, value); }

        public EditorViewModel(PackFileService pf, CopyPasteManager copyPasteManager)
        {
            _pf = pf;
            _copyPasteManager = copyPasteManager;
        }

        void Initialise(PackFile file)
        {
            if (file == _file)
                return;

            _file = file;
            Tags.Clear();
            DisplayName.Value = file == null ? "" : file.Name;

            if (file == null)
                return;

            var fileContent = _file.DataSource.ReadData();

            var parser = new MetaDataFileParser();
            _metaDataFile = parser.ParseFile(fileContent);

            foreach (var item in _metaDataFile.Items)
            {
                if (item is UnknownMetaEntry uknMeta)
                    Tags.Add(new UnkMetaDataTagItemViewModel(uknMeta));
                else if (item is BaseMetaEntry metaBase)
                    Tags.Add(new MetaDataTagItemViewModel(metaBase));
                else
                    throw new System.Exception();
            }

        }

        public void MoveUpAction()
        {
            var itemToMove = SelectedTag;
            if (itemToMove == null)
                return;

            var currentIndex = Tags.IndexOf(itemToMove);
            if (currentIndex == 0)
                return;

            Tags.Remove(itemToMove);
            Tags.Insert(currentIndex - 1, itemToMove);

            SelectedTag = itemToMove;
        }

        public void MoveDownAction()
        {
            var itemToMove = SelectedTag;
            if (itemToMove == null)
                return;

            var currentIndex = Tags.IndexOf(itemToMove);
            if (currentIndex == Tags.Count - 1)
                return;

            Tags.Remove(itemToMove);
            Tags.Insert(currentIndex + 1, itemToMove);

            SelectedTag = itemToMove;
        }

        public void DeleteAction()
        {
            if (SelectedTag == null)
                return;

            Tags.Remove(SelectedTag);
            SelectedTag = Tags.FirstOrDefault();
        }

        public void NewAction()
        {
            var dialog = new NewTagWindow();
            var allDefs = MetaDataTagDeSerializer.GetSupportedTypes();

            NewTagWindowViewModel model = new NewTagWindowViewModel();
            model.Items = new ObservableCollection<string>(allDefs);
            dialog.DataContext = model;

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value == true)
            {
                var newEntry = MetaDataTagDeSerializer.CreateDefault(model.SelectedItem);
                var newTagView = new MetaDataTagItemViewModel(newEntry);
                Tags.Add(newTagView);
            }

            dialog.DataContext = null;
        }

        public void PasteAction()
        {
            var pasteObjList = _copyPasteManager.GetPasteObjects<ICopyPastItem>();

            if (pasteObjList != null)
            {
                var pasteObjects = pasteObjList.ToList().ConvertAll(x => x as MetaDataTagCopyItem);
                var confirm = MessageBox.Show($"Paste {pasteObjects.Count} metadata objects?", "paste milord?", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                foreach (var item in pasteObjects)
                {
                    try
                    {
                        var typed = MetaDataTagDeSerializer.DeSerialize(item.Data, out var errorStr);
                        if (typed == null)
                            throw new System.Exception(errorStr);
                        Tags.Add(new MetaDataTagItemViewModel(typed));
                    }
                    catch
                    {
                        Tags.Add(new UnkMetaDataTagItemViewModel(item.Data));
                    }
                }
                return;
            }


            var pasteObject = _copyPasteManager.GetPasteObject<MetaDataTagCopyItem>();
            if (pasteObject == null)
            {
                MessageBox.Show("No valid object found to paste");
                return;
            }

            try
            {
                var typed = MetaDataTagDeSerializer.DeSerialize(pasteObject.Data, out var errorStr);
                if (typed == null)
                    throw new System.Exception(errorStr);
                Tags.Add(new MetaDataTagItemViewModel(typed));
            }
            catch
            {
                Tags.Add(new UnkMetaDataTagItemViewModel(pasteObject.Data));
            }
        }

        public void PickCopyMultiple()
        {
            if (SelectedTag == null)
                return;

            if (SelectedTag == _prevSelectedTag)
                return;

            _prevSelectedTag = SelectedTag;

            _selectedTags.Add(SelectedTag);
        }

        public void ClearCopyMultiple()
        {
            _prevSelectedTag = null;
            _selectedTags.Clear();
            _copyPasteManager.Clear();
        }

        public void CopyAction()
        {
            if (SelectedTag == null)
                return;

            if (_selectedTags.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(SelectedTag.HasError()) == false)
                {
                    MessageBox.Show($"Can not copy object due to: {SelectedTag.HasError()}");
                    return;
                }

                var tag = SelectedTag.GetAsData();
                var copyItem = new MetaDataTagCopyItem()
                {
                    Data = new UnknownMetaEntry()
                    {
                        Name = tag.Name,
                        Data = tag.DataItem.Bytes,
                        Version = SelectedTag.Version.Value,
                    }
                };
                _copyPasteManager.SetCopyItem(copyItem);
            }
            else
            {
                foreach (var item in _selectedTags)
                {

                    if (string.IsNullOrWhiteSpace(item.HasError()) == false)
                    {
                        MessageBox.Show($"Can not copy object due to: {item.HasError()}");
                        return;
                    }
                }

                var itemsToCopy = new List<ICopyPastItem>();

                foreach (var item in _selectedTags)
                {
                    var tag = item.GetAsData();
                    var copyItem = new MetaDataTagCopyItem()
                    {
                        Data = new UnknownMetaEntry()
                        {
                            Name = tag.Name,
                            Data = tag.DataItem.Bytes,
                            Version = SelectedTag.Version.Value,
                        }
                    };
                    itemsToCopy.Add(copyItem);
                }

                _copyPasteManager.SetCopyItems(itemsToCopy);
                MessageBox.Show($"copied {itemsToCopy.Count} metadata tag, milord!");
            }

        }

        public bool SaveAction()
        {
            var path = _pf.GetFullPath(_file);

            foreach (var tag in Tags)
            {
                var currentErrorMessage = tag.HasError();
                if (string.IsNullOrWhiteSpace(currentErrorMessage) == false)
                {
                    MessageBox.Show($"Unable to save : {currentErrorMessage}");
                    return false;
                }
            }

            _logger.Here().Information("Creating metadata file. TagCount=" + Tags.Count + " " + path);
            var tagDataItems = new List<MetaDataTagItem>();

            foreach (var tag in Tags)
            {
                _logger.Here().Information("Prosessing tag " + tag?.DisplayName?.Value);
                tagDataItems.Add(tag.GetAsData());
            }

            _logger.Here().Information("Generating bytes");

            MetaDataFileParser parser = new MetaDataFileParser();
            var bytes = parser.GenerateBytes(_metaDataFile.Version, tagDataItems);
            _logger.Here().Information("Saving");
            var res = SaveHelper.Save(_pf, path, null, bytes);
            if (res != null)
            {
                _file = res;
                DisplayName.Value = _file.Name;
            }

            _logger.Here().Information("Creating metadata file complete");
            EditorSavedEvent?.Invoke(_file);
            return _file != null;
        }

        public bool HasUnsavedChanges { get; set; } = false;
        public bool Save() => SaveAction();
        public void Close() { }

    }
}

