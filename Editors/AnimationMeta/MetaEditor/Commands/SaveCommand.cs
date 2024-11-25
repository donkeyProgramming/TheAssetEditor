using System.Collections.Generic;
using System.Windows;
using Editors.AnimationMeta.Presentation;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Scoped;
using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    class SaveCommand : IUiCommand
    {
        private readonly ILogger _logger = Logging.Create<SaveCommand>();
        private readonly IPackFileService _packFileService;
        private readonly IEventHub _eventHub;
        private readonly IFileSaveService _packFileSaveService;

        public SaveCommand(IPackFileService packFileService, IEventHub eventHub, IFileSaveService packFileSaveService)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
            _packFileSaveService = packFileSaveService;
        }

        public bool Execute(MetaDataEditorViewModel controller)
        {
            var path = _packFileService.GetFullPath(controller.CurrentFile);
            foreach (var tag in controller.Tags)
            {
                var currentErrorMessage = tag.HasError();
                if (string.IsNullOrWhiteSpace(currentErrorMessage) == false)
                {
                    MessageBox.Show($"Unable to save : {currentErrorMessage}");
                    return false;
                }
            }

            _logger.Here().Information("Creating metadata file. TagCount=" + controller.Tags.Count + " " + path);
            var tagDataItems = new List<MetaDataTagItem>();
            foreach (var tag in controller.Tags)
            {
                _logger.Here().Information("Prosessing tag " + tag?.DisplayName);
                tagDataItems.Add(tag.GetAsFileFormatData());
            }

            _logger.Here().Information("Generating bytes");

            var parser = new MetaDataFileParser();
            var bytes = parser.GenerateBytes(controller.MetaDataFileVersion, tagDataItems);
            _logger.Here().Information("Saving");
            var res = _packFileSaveService.Save(path, bytes, false);
            if (res != null)
            {
                controller.CurrentFile = res;
                controller.DisplayName = res.Name;
            }

            _logger.Here().Information("Creating metadata file complete");
            var saveEvent = new ScopedFileSavedEvent()
            {
                FileOwner = controller,
                NewPath = path,
            };
            _eventHub.Publish(saveEvent);

            return true;
        }
    }
}

