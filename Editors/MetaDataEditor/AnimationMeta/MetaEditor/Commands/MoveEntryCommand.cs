using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class MoveEntryCommand : IUiCommand
    {
        public void ExecuteUp(MetaDataEditorViewModel controller)
        {
            if (controller.ParsedFile == null) return;

            var itemsToMove = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (!itemsToMove.Any()) return;

            var attributes = controller.ParsedFile.Attributes;

            // Sort ascending to move top items first, preventing them from jumping over each other
            var sortedItems = itemsToMove.OrderBy(x => attributes.IndexOf(x)).ToList();
            bool moved = false;

            foreach (var item in sortedItems)
            {
                var currentIndex = attributes.IndexOf(item);
                if (currentIndex > 0)
                {
                    // If the item above is also in the selection, keep them as a block
                    var itemAbove = attributes[currentIndex - 1];
                    if (!itemsToMove.Contains(itemAbove))
                    {
                        attributes.RemoveAt(currentIndex);
                        attributes.Insert(currentIndex - 1, item);
                        moved = true;
                    }
                }
            }

            if (moved)
            {
                controller.UpdateView();

                // Restore selection state for the moved items
                foreach (var tag in controller.Tags.Where(t => itemsToMove.Contains(t._input)))
                {
                    tag.IsSelected = true;
                }

                controller.SelectedTag = controller.Tags.FirstOrDefault(x => x.IsSelected);
            }
        }

        public void ExecuteDown(MetaDataEditorViewModel controller)
        {
            if (controller.ParsedFile == null) return;

            var itemsToMove = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (!itemsToMove.Any()) return;

            var attributes = controller.ParsedFile.Attributes;

            // Sort descending to move bottom items first
            var sortedItems = itemsToMove.OrderByDescending(x => attributes.IndexOf(x)).ToList();
            bool moved = false;

            foreach (var item in sortedItems)
            {
                var currentIndex = attributes.IndexOf(item);
                if (currentIndex < attributes.Count - 1)
                {
                    // If the item below is also in the selection, keep them as a block
                    var itemBelow = attributes[currentIndex + 1];
                    if (!itemsToMove.Contains(itemBelow))
                    {
                        attributes.RemoveAt(currentIndex);
                        attributes.Insert(currentIndex + 1, item);
                        moved = true;
                    }
                }
            }

            if (moved)
            {
                controller.UpdateView();

                // Restore selection state for the moved items
                foreach (var tag in controller.Tags.Where(t => itemsToMove.Contains(t._input)))
                {
                    tag.IsSelected = true;
                }

                controller.SelectedTag = controller.Tags.FirstOrDefault(x => x.IsSelected);
            }
        }
    }
}
