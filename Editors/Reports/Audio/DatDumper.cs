using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Dat;

namespace Editors.Reports.Audio
{
    public class GenerateDatDumperReportCommand(DatDumper generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class DatDumper
    {
        private readonly IPackFileService _packFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public DatDumper(IPackFileService packFileService, ApplicationSettingsService applicationSettingsService)
        {
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
        }

        public static void Generate(IPackFileService packFileService, ApplicationSettingsService applicationSettingsService)
        {
            var instance = new DatDumper(packFileService, applicationSettingsService);
            instance.Create();
        }

        public void Create()
        {
            var datDumper = new DatDumper(_packFileService, _applicationSettingsService);
            datDumper.LoadDatFiles(_packFileService, _applicationSettingsService, out var _);
        }

        private void LoadDatFiles(IPackFileService packFileService, ApplicationSettingsService applicationSettingsService, out List<string> failedFiles)
        {
            var reportsFolderName = $"{DirectoryHelper.ReportsDirectory}\\";
            DirectoryHelper.EnsureCreated(reportsFolderName);

            var datFiles = PackFileServiceUtility.FindAllWithExtention(packFileService, ".dat");

            var failedDatParsing = new List<(string, string)>();
            var masterDat = new SoundDatFile();

            foreach (var datFile in datFiles)
            {
                var datDump = $"{reportsFolderName}\\dat_dump_{datFile}.txt";
                try
                {
                    var parsedFile = LoadDatFile(datFile);
                    masterDat.Merge(parsedFile);
                    parsedFile.DumpToFile(datDump);
                }
                catch (Exception e)
                {
                    var fullPath = packFileService.GetFullPath(datFile);
                    failedDatParsing.Add((fullPath, e.Message));
                }
            }

            failedFiles = failedDatParsing.Select(x => x.Item1).ToList();

            var masterDatDump = $"{reportsFolderName}\\dat_dump_master.txt";
            masterDat.DumpToFile(masterDatDump);
        }

        private SoundDatFile LoadDatFile(PackFile datFile)
        {
            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Attila)
                return DatFileParser.Parse(datFile, true);
            else
                return DatFileParser.Parse(datFile, false);
        }
    }
}
