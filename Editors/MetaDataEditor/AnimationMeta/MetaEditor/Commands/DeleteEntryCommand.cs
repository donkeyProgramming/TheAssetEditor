using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class DeleteEntryCommand : IUiCommand
    {
        public void Execute(MetaDataEditorViewModel controller)
        {
            if (controller?.ParsedFile == null)
                return;

            var itemsToRemove = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (itemsToRemove.Count == 0)
                return;

            foreach (var item in itemsToRemove)
            {
                controller.ParsedFile.Attributes.Remove(item);
            }

            controller.UpdateView();
        }
    }
}
