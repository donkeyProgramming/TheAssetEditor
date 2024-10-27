using System;
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
        public List<UnknownMetaEntry> Items { get; set; } = [];
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
            var selectedTags = controller.Tags
                .Where(x => x.IsSelected)
                .ToList();

            // Check for errors
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
                var entry = new UnknownMetaEntry()
                {
                    Name = fileFormatData.Name,
                    Data = fileFormatData.DataItem.Bytes,
                    Version = tag.Version,
                };
                copyPastItem.Items.Add(entry);
            }
            _copyPasteManager.SetCopyItem(copyPastItem);
        }

        public void ExecutePaste(MetaDataEditorViewModel controller)
        {
            var pastObject = _copyPasteManager.GetPasteObject<MetaDataTagCopyItem>();
            if (pastObject != null)
            {
                var pasteObjects = pastObject.Items;
                var confirm = MessageBox.Show($"Paste {pasteObjects.Count} metadata objects?", "Meta Copy Paste", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) 
                    return;

                foreach (var item in pasteObjects)
                {
                    try
                    {
                        var typed = MetaDataTagDeSerializer.DeSerialize(item, out var errorStr);
                        if (typed == null)
                            throw new Exception(errorStr);
                        controller.Tags.Add(new MetaDataEntry(typed));
                    }
                    catch
                    {
                        controller.Tags.Add(new UnkMetaDataEntry(item));
                    }
                }
                return;
            }

        }


       

    }
}

