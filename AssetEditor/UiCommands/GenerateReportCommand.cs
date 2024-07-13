using Editors.Reports;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace AssetEditor.UiCommands
{
    public class GenerateReportCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly GameInformationFactory _gameInformationFactory;

        public GenerateReportCommand(PackFileService packFileService, ApplicationSettingsService applicationSettingsService, GameInformationFactory gameInformationFactory)
        {
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
            _gameInformationFactory = gameInformationFactory;
        }

        public void Rmv2()
        {
            var gameName = _gameInformationFactory.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).DisplayName;
            var reportGenerator = new Rmv2ReportGenerator(_packFileService);
            reportGenerator.Create(gameName);
        }
        public void MetaData() => AnimMetaDataReportGenerator.Generate(_packFileService, _applicationSettingsService, _gameInformationFactory);
        public void FileList() => FileListReportGenerator.Generate(_packFileService, _applicationSettingsService, _gameInformationFactory);
        public void MetaDataJson() => AnimMetaDataJsonGenerator.Generate(_packFileService, _applicationSettingsService, _gameInformationFactory);
        public void Material() => new MaterialReportGenerator(_packFileService).Create();
    }
}
