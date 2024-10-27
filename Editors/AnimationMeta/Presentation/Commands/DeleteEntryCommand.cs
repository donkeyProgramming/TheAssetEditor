using System.Linq;
using Shared.Core.Events;

namespace Editors.AnimationMeta.Presentation.Commands
{
    internal class DeleteEntryCommand : IUiCommand
    {

        public void Execute(MetaDataEditorViewModel controller)
        {
            if (controller.SelectedTag == null)
                return;

            controller.Tags.Remove(controller.SelectedTag);
            controller.SelectedTag = controller.Tags.FirstOrDefault();
        }

    }
}
