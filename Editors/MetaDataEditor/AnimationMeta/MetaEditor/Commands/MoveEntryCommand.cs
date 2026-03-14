using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class MoveEntryCommand : IUiCommand
    {
        public void ExecuteUp(MetaDataEditorViewModel controller)
        {
            var itemToMove = controller.SelectedAttribute;
            if (itemToMove == null || controller.ParsedFile == null)
                return;

             var currentIndex = controller.ParsedFile.Attributes.IndexOf(itemToMove);
             if (currentIndex == 0)
                 return;

            controller.ParsedFile.Attributes.Remove(itemToMove);
            controller.ParsedFile.Attributes.Insert(currentIndex - 1, itemToMove);
            controller.UpdateView();
            controller.SelectedTag = controller.Tags
                .Where(x => x._input == itemToMove)
                .FirstOrDefault();
        }

        public void ExecuteDown(MetaDataEditorViewModel controller)
        {
            var itemToMove = controller.SelectedAttribute;
            if (itemToMove == null || controller.ParsedFile == null)
                return;

            var currentIndex = controller.ParsedFile.Attributes.IndexOf(itemToMove);
            if (currentIndex == controller.ParsedFile.Attributes.Count -1)
                return;

            controller.ParsedFile.Attributes.Remove(itemToMove);
            controller.ParsedFile.Attributes.Insert(currentIndex + 1, itemToMove);
            controller.UpdateView();
            controller.SelectedTag = controller.Tags
                .Where(x => x._input == itemToMove)
                .FirstOrDefault();

        }
    }
}
