using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class DeleteEntryCommand : IUiCommand
    {
        public void Execute(MetaDataEditorViewModel controller)
        {
            var itemToRemove = controller.SelectedAttribute;
            if (itemToRemove == null || controller.ParsedFile == null)
                return;

            controller.ParsedFile.Attributes.Remove(itemToRemove);
            controller.UpdateView();
            controller.SelectedTag = controller.Tags.FirstOrDefault();
        }

    }
}
