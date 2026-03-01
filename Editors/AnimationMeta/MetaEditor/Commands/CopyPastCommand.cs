using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    public class MetaDataTagCopyItem : ICopyPastItem
    {
        public string Description => "Copy object for MetaDataTag";
        public List<ParsedMetadataAttribute> Items { get; set; } = [];
    }

    class CopyPastCommand : IUiCommand
    {
        private readonly CopyPasteManager _copyPasteManager;
        private readonly MetaDataFileParser _metaDataFileParser;

        public CopyPastCommand(CopyPasteManager copyPasteManager, MetaDataFileParser metaDataFileParser)
        {
            _copyPasteManager = copyPasteManager;
            _metaDataFileParser = metaDataFileParser;
        }

        public void ExecuteCopy(MetaDataEditorViewModel controller)
        {
            var selectedTags = controller.Tags
                .Where(x => x.IsSelected)
                .ToList();

            // Check for errors

            foreach (var tag in selectedTags)
            {
                if (string.IsNullOrWhiteSpace(tag.HasError()) == false || tag._input == null)
                {
                    MessageBox.Show($"Can not copy object due to: {tag.HasError()}");
                    return;
                }
            }
            var copyPastItem = new MetaDataTagCopyItem();
            foreach (var item in selectedTags)
            {
                var copy = ReflectionHelper.CreateShallowCopy(item._input);
                copyPastItem.Items.Add(copy);

            }
            _copyPasteManager.SetCopyItem(copyPastItem);
        }

        public void ExecutePaste(MetaDataEditorViewModel controller)
        {
            var pastObject = _copyPasteManager.GetPasteObject<MetaDataTagCopyItem>();
            if (pastObject != null)
            {
                controller.ParsedFile.Attributes.AddRange(pastObject.Items);
            }

            controller.UpdateView();
        }
    }
}

