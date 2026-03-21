using System.Linq;
using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class DeleteEntryCommand : IUiCommand
    {
        public void Execute(MetaDataEditorViewModel controller)
        {
            if (controller.ParsedFile == null) return;

            // Get all selected items from the UI tags
            var itemsToRemove = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (!itemsToRemove.Any()) return;

            // Batch remove
            foreach (var item in itemsToRemove)
            {
                controller.ParsedFile.Attributes.Remove(item);
            }

            controller.UpdateView();
            controller.SelectedTag = controller.Tags.FirstOrDefault();
        }
    }
}
