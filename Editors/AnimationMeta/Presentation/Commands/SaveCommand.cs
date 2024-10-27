using System.Collections.Generic;
using System.Windows;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation.Commands
{
    class SaveCommand : IUiCommand
    {
        private readonly ILogger _logger = Logging.Create<SaveCommand>();
        private readonly PackFileService _packFileService;
        private readonly EventHub _eventHub;

        public SaveCommand(PackFileService packFileService, EventHub eventHub)
        {
            _packFileService = packFileService;
            _eventHub = eventHub;
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
            var res = SaveHelper.Save(_packFileService, path, null, bytes);
            if (res != null)
            {
                controller.CurrentFile = res;
                controller.DisplayName = res.Name;
            }

            _logger.Here().Information("Creating metadata file complete");
            //EditorSavedEvent?.Invoke(_file);
            //_eventHub.Publish<PackFileSavedEvent>

            return true;
        }
    }
}

