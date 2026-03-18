using CommunityToolkit.Diagnostics;
using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.Services;
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
        private readonly IStandardDialogs _standardDialogs;

        public CopyPastCommand(CopyPasteManager copyPasteManager, IStandardDialogs standardDialogs)
        {
            _copyPasteManager = copyPasteManager;
            _standardDialogs = standardDialogs;
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
                    _standardDialogs.ShowDialogBox($"Can not copy object due to: {tag.HasError()}");
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
            Guard.IsNotNull(controller.ParsedFile, $"{nameof(controller.ParsedFile)} - Can not paste when no file is loaded");

            var pastObject = _copyPasteManager.GetPasteObject<MetaDataTagCopyItem>();
            if (pastObject != null)
                controller.ParsedFile.Attributes.AddRange(pastObject.Items);

            controller.UpdateView();
        }
    }
}

