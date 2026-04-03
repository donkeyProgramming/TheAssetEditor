using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class DeleteEntryCommand : IUiCommand
    {
        public void Execute(MetaDataEditorViewModel controller)
        {
            // Validate controller state
            if (controller?.ParsedFile == null)
                return;

            // Support both multi-selection (IsSelected) and single selection (SelectedTag)
            // This ensures compatibility with different UI interaction patterns
            var itemsToRemove = GetSelectedItems(controller);

            if (itemsToRemove.Count == 0)
                return;

            // Batch remove selected items from the underlying data structure
            // Using ToList() to avoid collection modification during enumeration
            foreach (var item in itemsToRemove)
            {
                controller.ParsedFile.Attributes.Remove(item);
            }

            // Refresh the view to reflect changes
            controller.UpdateView();

            // Set default selection to the first available item
            // This prevents UI from being in an undefined state
            if (controller.Tags.Count > 0)
            {
                controller.SelectedTag = controller.Tags[0];
            }
            else
            {
                controller.SelectedTag = null;
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
    }
}
