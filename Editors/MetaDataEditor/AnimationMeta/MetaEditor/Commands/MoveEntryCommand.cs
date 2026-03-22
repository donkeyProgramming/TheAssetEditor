using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class MoveEntryCommand : IUiCommand
    {
        public void ExecuteUp(MetaDataEditorViewModel controller)
        {
            // Validate controller state
            if (controller?.ParsedFile == null)
                return;

            var itemsToMove = GetSelectedItems(controller);

            if (itemsToMove.Count == 0)
                return;

            var attributes = controller.ParsedFile.Attributes;

            // Sort ascending: move top items first to maintain relative order
            // This prevents items from jumping over each other during batch move
            var sortedItems = itemsToMove
                .OrderBy(x => attributes.IndexOf(x))
                .ToList();

            bool moved = false;

            foreach (var item in sortedItems)
            {
                var currentIndex = attributes.IndexOf(item);

                // Boundary check: can't move up if already at top
                if (currentIndex <= 0)
                    continue;

                var itemAbove = attributes[currentIndex - 1];

                // If the item above is also selected, skip to maintain selection block
                // This keeps multi-selected items together as a group
                if (itemsToMove.Contains(itemAbove))
                    continue;

                // Perform the move operation
                attributes.RemoveAt(currentIndex);
                attributes.Insert(currentIndex - 1, item);
                moved = true;
            }

            if (moved)
            {
                // Refresh view and restore selection state
                controller.UpdateView();
                RestoreSelection(controller, itemsToMove);
            }
        }

        public void ExecuteDown(MetaDataEditorViewModel controller)
        {
            // Validate controller state
            if (controller?.ParsedFile == null)
                return;

            var itemsToMove = GetSelectedItems(controller);

            if (itemsToMove.Count == 0)
                return;

            var attributes = controller.ParsedFile.Attributes;

            // Sort descending: move bottom items first to maintain relative order
            // This prevents items from jumping over each other during batch move
            var sortedItems = itemsToMove
                .OrderByDescending(x => attributes.IndexOf(x))
                .ToList();

            bool moved = false;

            foreach (var item in sortedItems)
            {
                var currentIndex = attributes.IndexOf(item);

                // Boundary check: can't move down if already at bottom
                if (currentIndex < 0 || currentIndex >= attributes.Count - 1)
                    continue;

                var itemBelow = attributes[currentIndex + 1];

                // If the item below is also selected, skip to maintain selection block
                // This keeps multi-selected items together as a group
                if (itemsToMove.Contains(itemBelow))
                    continue;

                // Perform the move operation
                attributes.RemoveAt(currentIndex);
                attributes.Insert(currentIndex + 1, item);
                moved = true;
            }

            if (moved)
            {
                // Refresh view and restore selection state
                controller.UpdateView();
                RestoreSelection(controller, itemsToMove);
            }
        }

        /// <summary>
        /// Retrieves selected items supporting both multi-selection and single-selection modes.
        /// Priority: IsSelected (multi) > SelectedTag (single)
        /// </summary>
        private List<ParsedMetadataAttribute> GetSelectedItems(MetaDataEditorViewModel controller)
        {
            // First, check for multi-selection via IsSelected flag
            var multiSelected = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (multiSelected.Count > 0)
                return multiSelected;

            // Fallback to single selection via SelectedTag
            // This ensures backward compatibility with existing tests and single-select scenarios
            if (controller.SelectedTag != null)
            {
                return new List<ParsedMetadataAttribute> { controller.SelectedTag._input };
            }

            return new List<ParsedMetadataAttribute>();
        }

        /// <summary>
        /// Restores the selection state after a move operation.
        /// Sets IsSelected = true for all moved items and updates SelectedTag.
        /// </summary>
        private void RestoreSelection(MetaDataEditorViewModel controller, List<ParsedMetadataAttribute> movedItems)
        {
            // Restore IsSelected state for all moved items
            foreach (var tag in controller.Tags)
            {
                if (movedItems.Contains(tag._input))
                {
                    tag.IsSelected = true;
                }
            }

            // Update SelectedTag to the first selected item
            // This maintains consistency with UI expectations
            controller.SelectedTag = controller.Tags.FirstOrDefault(x => x.IsSelected);
        }
    }
}
