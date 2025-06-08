using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class CopyRowsCommand : IUiCommand
    {
        private readonly IEventHub _eventHub;

        public CopyRowsCommand(IEventHub eventHub)
        {
            _eventHub = eventHub;
        }

        public void Execute()
        {
            _eventHub.Publish(new CopyRowsEvent());
        }
    }
}
