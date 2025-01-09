using System.IO;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Editors.Audio.BnkCompiler.ObjectGeneration;
using Editors.Audio.Storage;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Dat;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.BnkCompiler
{
    public class CompileResult
    {
        public CompilerData Project { get; set; }
        public PackFile OutputBnkFile { get; set; }
        public PackFile OutputEventDatFile { get; set; }
        public PackFile OutputStateDatFile { get; set; }
    }

    public class Compiler
    {
        private readonly HircBuilder _hircBuilder;
        private readonly BnkHeaderBuilder _headerBuilder;
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;

        public Compiler(HircBuilder hircBuilder, BnkHeaderBuilder headerBuilder, IAudioRepository audioRepository, IPackFileService packFileService)
        {
            _hircBuilder = hircBuilder;
            _headerBuilder = headerBuilder;
            _audioRepository = audioRepository;
            _packFileService = packFileService;
        }

        public Result<CompileResult> CompileProject(CompilerData audioProject, ApplicationSettingsService applicationSettingsService)
        {
            // Build the wwise object graph. 
            var header = BnkHeaderBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);

            // Ensure all write ids are not causing conflicts.
            var allIds = hircChunk.HircItems.Select(x => x.Id).ToList();
            var originalCount = allIds.Count;
            var uniqueCount = allIds.Distinct().Count();
            Guard.IsEqualTo(originalCount, uniqueCount);

            // Build the dat files.
            var eventDat = audioProject.Events.Count == 0 ? null : BuildEventDataDat(audioProject);
            var statesDat = audioProject.DialogueEvents.Count == 0 ? null : BuildStateDataDat(audioProject);

            var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
            var gameBankGeneratorVersion = (uint)gameInformation.BankGeneratorVersion;

            var compileResult = new CompileResult()
            {
                Project = audioProject,
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName, gameBankGeneratorVersion),
                OutputEventDatFile = eventDat,
                OutputStateDatFile = statesDat,
            };

            return Result<CompileResult>.FromOk(compileResult);
        }

        private static PackFile ConvertToPackFile(BkhdChunk header, HircChunk hircChunk, string outputFile, uint gameBankGeneratorVersion)
        {
            var outputName = $"{outputFile}.bnk";
            var headerBytes = BkhdParser.WriteData(header);
            var hircBytes = new HircParser().WriteData(hircChunk, gameBankGeneratorVersion);

            // Write
            using var memStream = new MemoryStream();
            memStream.Write(headerBytes);
            memStream.Write(hircBytes);
            var bytes = memStream.ToArray();

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile(outputName, new MemorySource(bytes));
            var parser = new BnkParser();
            var reparsedSanityFile = parser.Parse(bnkPackFile, "test\\fakefilename.bnk", true);
            return bnkPackFile;
        }

        private static PackFile BuildEventDataDat(CompilerData projectFile)
        {
            var outputName = $"event_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.StatesDat)
                datFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = wwiseEvent, Value = 400 });

            var bytes = DatFileParser.WriteData(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }

        private static PackFile BuildStateDataDat(CompilerData projectFile)
        {
            var outputName = $"state_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var state in projectFile.StatesDat)
                datFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = state, Value = 400 });

            var bytes = DatFileParser.WriteData(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }
    }
}
