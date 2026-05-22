using CommonControls.Editors.AnimationPack;
using Shared.Core.Events;

namespace Editors.AnimationFragmentEditor.AnimationPack.Commands
{
    public class RemoveSelectedFileCommand : IAeCommand
    {
        private AnimPackViewModel _editor = null!;

        public void Configure(AnimPackViewModel editor)
        {
            _editor = editor;
        }

        public void Execute()
        {
            _editor.AnimationPackItems.PossibleValues.Remove(_editor.AnimationPackItems.SelectedItem);
            _editor.AnimationPackItems.RefreshFilter();
        }
    }

}
