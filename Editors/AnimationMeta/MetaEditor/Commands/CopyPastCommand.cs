using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation.Commands
{

    public class MetaDataTagCopyItem : ICopyPastItem
    {
        public string Description => "Copy object for MetaDataTag";
        public List<UnknownMetaEntry> Data { get; set; } = [];
    }

    class CopyPastCommand : IUiCommand
    {
        private readonly CopyPasteManager _copyPasteManager;

        public CopyPastCommand(CopyPasteManager copyPasteManager)
        {
            _copyPasteManager = copyPasteManager;
        }

        public void ExecuteCopy(MetaDataEditorViewModel controller)
        {
            if (controller.SelectedTag == null)
                return;

            var selectedTags = controller.Tags
                .Where(x => x.IsSelected)
                .ToList();

            selectedTags.Add(controller.SelectedTag);
            selectedTags = selectedTags.Distinct().ToList();

            // Check for errors

            // Create copy item
            foreach (var tag in selectedTags)
            {
                if (string.IsNullOrWhiteSpace(tag.HasError()) == false)
                {
                    MessageBox.Show($"Can not copy object due to: {tag.HasError()}");
                    return;
                }
            }

            var copyPastItem = new MetaDataTagCopyItem();
            foreach (var tag in selectedTags)
            {
                var fileFormatData = tag.GetAsFileFormatData();
                var Data = new UnknownMetaEntry()
                {
                    Name = fileFormatData.Name,
                    Data = fileFormatData.DataItem.Bytes,
                    Version = tag.Version,
                };
            }
            _copyPasteManager.SetCopyItem(copyPastItem);
        }

        public void ExecutePaste(MetaDataEditorViewModel controller)
        { }


        /*
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

                var tag = SelectedTag.GetAsFileFormatData();
                var copyItem = new MetaDataTagCopyItem()
                {
                    Data = new UnknownMetaEntry()
                    {
                        Name = tag.Name,
                        Data = tag.DataItem.Bytes,
                        Version = SelectedTag.Version,
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
                    var tag = item.GetAsFileFormatData();
                    var copyItem = new MetaDataTagCopyItem()
                    {
                        Data = new UnknownMetaEntry()
                        {
                            Name = tag.Name,
                            Data = tag.DataItem.Bytes,
                            Version = SelectedTag.Version,
                        }
                    };
                    itemsToCopy.Add(copyItem);
                }

                _copyPasteManager.SetCopyItems(itemsToCopy);
                MessageBox.Show($"copied {itemsToCopy.Count} metadata tag, milord!");
            }

        }
         */

    }
}

