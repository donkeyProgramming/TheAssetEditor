using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class MoveEntryCommand : IUiCommand
    {
        public void ExecuteUp(MetaDataEditorViewModel controller)
        {
            if (controller?.ParsedFile == null)
                return;

            var itemsToMove = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (itemsToMove.Count == 0)
                return;

            var attributes = controller.ParsedFile.Attributes;

            // Sort ascending: move top items first to maintain relative order
            var sortedItems = itemsToMove
                .OrderBy(x => attributes.IndexOf(x))
                .ToList();

            bool moved = false;

            foreach (var item in sortedItems)
            {
                var currentIndex = attributes.IndexOf(item);

                if (currentIndex <= 0)
                    continue;

                var itemAbove = attributes[currentIndex - 1];

                // If the item above is also selected, skip to keep the block together
                if (itemsToMove.Contains(itemAbove))
                    continue;

                attributes.RemoveAt(currentIndex);
                attributes.Insert(currentIndex - 1, item);
                moved = true;
            }

            if (moved)
            {
                controller.UpdateView();
                RestoreSelection(controller, itemsToMove);
            }
        }

        public void ExecuteDown(MetaDataEditorViewModel controller)
        {
            if (controller?.ParsedFile == null)
                return;

            var itemsToMove = controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (itemsToMove.Count == 0)
                return;

            var attributes = controller.ParsedFile.Attributes;

            // Sort descending: move bottom items first to maintain relative order
            var sortedItems = itemsToMove
                .OrderByDescending(x => attributes.IndexOf(x))
                .ToList();

            bool moved = false;

            foreach (var item in sortedItems)
            {
                var currentIndex = attributes.IndexOf(item);

                if (currentIndex < 0 || currentIndex >= attributes.Count - 1)
                    continue;

                var itemBelow = attributes[currentIndex + 1];

                // If the item below is also selected, skip to keep the block together
                if (itemsToMove.Contains(itemBelow))
                    continue;

                attributes.RemoveAt(currentIndex);
                attributes.Insert(currentIndex + 1, item);
                moved = true;
            }

            if (moved)
            {
                controller.UpdateView();
                RestoreSelection(controller, itemsToMove);
            }
        }

        private void RestoreSelection(MetaDataEditorViewModel controller, List<ParsedMetadataAttribute> movedItems)
        {
            foreach (var tag in controller.Tags)
            {
                if (movedItems.Contains(tag._input))
                {
                    tag.IsSelected = true;
                }
            }
        }
    }
}
