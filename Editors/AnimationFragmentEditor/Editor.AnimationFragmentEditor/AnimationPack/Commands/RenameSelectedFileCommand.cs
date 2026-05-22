using CommonControls.BaseDialogs;
using CommonControls.Editors.AnimationPack;
using Shared.Core.Events;

namespace Editors.AnimationFragmentEditor.AnimationPack.Commands
{
    public class RenameSelectedFileCommand : IUiCommand
    { 
        public void Execute(AnimPackViewModel editor)
        {
            var animFile = editor.AnimationPackItems.PossibleValues.FirstOrDefault(file => file == editor.AnimationPackItems.SelectedItem);
            if (animFile == null)
                return;

            var window = new TextInputWindow("Rename Anim File", animFile.FileName);
            if (window.ShowDialog() == true)
                animFile.FileName = window.TextValue;

            // way to refresh the view
            editor.AnimationPackItems.RefreshFilter();
        }
    }

}
