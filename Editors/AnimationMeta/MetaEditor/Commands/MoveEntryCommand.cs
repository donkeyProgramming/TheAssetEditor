using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class MoveEntryCommand : IUiCommand
    {
        public void ExecuteUp(MetaDataEditorViewModel controller)
        {
            var itemToMove = controller.SelectedTag;
            if (itemToMove == null)
                return;

            var currentIndex = controller.Tags.IndexOf(itemToMove);
            if (currentIndex == 0)
                return;

            controller.Tags.Remove(itemToMove);
            controller.Tags.Insert(currentIndex - 1, itemToMove);

            controller.SelectedTag = itemToMove;
        }

        public void ExecuteDown(MetaDataEditorViewModel controller)
        {
            var itemToMove = controller.SelectedTag;
            if (itemToMove == null)
                return;

            var currentIndex = controller.Tags.IndexOf(itemToMove);
            if (currentIndex == controller.Tags.Count - 1)
                return;

            controller.Tags.Remove(itemToMove);
            controller.Tags.Insert(currentIndex + 1, itemToMove);

            controller.SelectedTag = itemToMove;
        }


    }
}
