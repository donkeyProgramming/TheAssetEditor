using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class DeleteEntryCommand : IAeCommand
    {
        private MetaDataEditorViewModel _controller = null!;

        public void Configure(MetaDataEditorViewModel controller)
        {
            _controller = controller;
        }

        public void Execute()
        {
            if (_controller?.ParsedFile == null)
                return;

            var itemsToRemove = _controller.Tags
                .Where(x => x.IsSelected)
                .Select(x => x._input)
                .ToList();

            if (itemsToRemove.Count == 0)
                return;

            foreach (var item in itemsToRemove)
            {
                _controller.ParsedFile.Attributes.Remove(item);
            }

            _controller.UpdateView();
        }
    }
}
