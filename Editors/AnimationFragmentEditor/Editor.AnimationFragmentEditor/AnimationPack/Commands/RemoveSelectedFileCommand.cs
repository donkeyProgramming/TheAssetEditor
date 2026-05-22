using CommonControls.Editors.AnimationPack;
using Shared.Core.Events;

namespace Editors.AnimationFragmentEditor.AnimationPack.Commands
{
    public class RemoveSelectedFileCommand : IUiCommand
    {
        public void Execute(AnimPackViewModel editor)
        {
            editor.AnimationPackItems.PossibleValues.Remove(editor.AnimationPackItems.SelectedItem);
            editor.AnimationPackItems.RefreshFilter();
        }

    }

}
