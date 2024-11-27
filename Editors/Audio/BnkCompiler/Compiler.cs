using System.IO;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Bkhd;
using Shared.GameFormats.WWise.Hirc;
using Shared.Core.PackFiles;
using Editors.Audio.BnkCompiler.ObjectGeneration;
using Editors.Audio.Storage;

namespace Editors.Audio.BnkCompiler
{
    public class CompileResult
    {
        public CompilerData Project { get; set; }
        public PackFile OutputBnkFile { get; set; }
        public PackFile OutputDatFile { get; set; }
        public PackFile OutputStatesDatFile { get; set; }
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

        public Result<CompileResult> CompileProject(CompilerData audioProject)
        {
            // Build the wwise object graph. 
            var header = BnkHeaderBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);

            // Ensure all write ids are not causing conflicts.
            var allIds = hircChunk.Hircs.Select(x => x.Id).ToList();
            var originalCount = allIds.Count();
            var uniqueCount = allIds.Distinct().Count();
            Guard.IsEqualTo(originalCount, uniqueCount);

            // Build the dat files.
            var eventDat = audioProject.Events.Count == 0 ? null : BuildDat(audioProject);
            var statesDat = audioProject.DialogueEvents.Count == 0 ? null : BuildStatesDat(audioProject);

            var compileResult = new CompileResult()
            {
                Project = audioProject,
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName),
                OutputDatFile = eventDat,
                OutputStatesDatFile = statesDat,
            };

            return Result<CompileResult>.FromOk(compileResult);
        }

        private PackFile ConvertToPackFile(BkhdHeader header, HircChunk hircChunk, string outputFile)
        {
            var outputName = $"{outputFile}.bnk";
            var headerBytes = BkhdParser.GetAsByteArray(header);
            var hircBytes = new HircParser().GetAsBytes(hircChunk);

            // Write
            using var memStream = new MemoryStream();
            memStream.Write(headerBytes);
            memStream.Write(hircBytes);
            var bytes = memStream.ToArray();

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile(outputName, new MemorySource(bytes));
            var parser = new BnkParser();
            var result = parser.Parse(bnkPackFile, "test\\fakefilename.bnk");

            return bnkPackFile;
        }

        private static PackFile BuildDat(CompilerData projectFile)
        {
            var outputName = $"event_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.StatesDat)
                datFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = wwiseEvent, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }

        private static PackFile BuildStatesDat(CompilerData projectFile)
        {
            var outputName = $"states_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var state in projectFile.StatesDat)
                datFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = state, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }
    }
}
