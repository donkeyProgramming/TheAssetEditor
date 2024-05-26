using System.IO;
using System.Linq;
using Audio.BnkCompiler.ObjectGeneration;
using Audio.FileFormats.Dat;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using CommunityToolkit.Diagnostics;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Audio.Utility;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Audio.FileFormats.WWise.Hirc.Shared;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Audio.Storage;

namespace Audio.BnkCompiler
{
    public class CompileResult
    {
        public CompilerData Project { get; set; }
        public PackFile OutputBnkFile { get; set; }
        public PackFile OutputDatFile { get; set; }
        public PackFile OutputStatesDatFile { get; set; }
    }

    internal static class CompilerConstants
    {
        public static readonly string Game_Warhammer3 = "Warhammer3";
    }

    public class Compiler
    {
        private readonly HircBuilder _hircBuilder;
        private readonly BnkHeaderBuilder _headerBuilder;
        private readonly RepositoryProvider _provider;

        public Compiler(HircBuilder hircBuilder, BnkHeaderBuilder headerBuilder, RepositoryProvider provider)
        {
            _hircBuilder = hircBuilder;
            _headerBuilder = headerBuilder;
            _provider = provider;
        }

        public Result<CompileResult> CompileProject(CompilerData audioProject)
        {
            // Load audio repository to access dat dump.
            var audioRepository = new AudioRepository(_provider, false);

            var dialogueEventStateGroups = new DialogueEventData(audioRepository);
            dialogueEventStateGroups.ExtractDialogueEventsFromDat();

            // Build the wwise object graph 
            var header = _headerBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);

            //Ensure all write ids are not causing conflicts. Todo, this will cause issues with reuse of sounds
            var allIds = hircChunk.Hircs.Select(x => x.Id).ToList();
            var originalCount = allIds.Count();
            var uniqueCount = allIds.Distinct().Count();
            Guard.IsEqualTo(originalCount, uniqueCount);

            var eventDat = (audioProject.Events.Count == 0) ? null : BuildDat(audioProject);
            var statesDat = (audioProject.DialogueEvents.Count == 0) ? null : BuildStatesDat(audioProject);

            var compileResult = new CompileResult()
            {
                Project = audioProject,
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName),
                OutputDatFile = eventDat,
                OutputStatesDatFile = statesDat,
            };

            return Result<CompileResult>.FromOk(compileResult);
        }

        PackFile ConvertToPackFile(BkhdHeader header, HircChunk hircChunk, string outputFile)
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

        PackFile BuildDat(CompilerData projectFile)
        {
            var outputName = $"event_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.Events)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = wwiseEvent.Name, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }

        PackFile BuildStatesDat(CompilerData projectFile)
        {
            var outputName = $"states_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var state in projectFile.DatStates)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = state, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }
    }
}
