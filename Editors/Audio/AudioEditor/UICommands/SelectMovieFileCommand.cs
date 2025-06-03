using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class SelectMovieFileCommand : IUiCommand
    {
        private readonly IStandardDialogs _standardDialogs;
        private readonly IEventHub _eventHub;
        private readonly IPackFileService _packFileService;

        public SelectMovieFileCommand(IStandardDialogs standardDialogs, IEventHub eventHub, IPackFileService packFileService)
        {
            _standardDialogs = standardDialogs;
            _eventHub = eventHub;
            _packFileService = packFileService;
        }

        public void Execute()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".ca_vp8"]);
            if (result.Result)
            {
                var movieFilePath = _packFileService.GetFullPath(result.File);
                _eventHub.Publish(new SelectMovieFileEvent(movieFilePath));
            }
        }
    }
}
