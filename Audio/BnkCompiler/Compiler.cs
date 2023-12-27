using Audio.BnkCompiler.ObjectGeneration;
using Audio.FileFormats.Dat;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommunityToolkit.Diagnostics;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler
{
    public class CompileResult
    {
        public CompilerData Project { get; set; }
        public PackFile OutputBnkFile { get; set; }
        public PackFile OutputDatFile { get; set; }
        public PackFile NameList { get; set; }
    }

    internal static class CompilerConstants
    {
        public static readonly string Game_Warhammer3 = "Warhammer3";
        public static readonly string Language_English = "English(uk)";
        public static readonly string Mixers_Diplomacy = "54848735";

        public static readonly string[] Possible_Languages = new string[] { Language_English };
    }

    public class Compiler
    {
        private readonly HichBuilder _hircBuilder;
        private readonly BnkHeaderBuilder _headerBuilder;

        public Compiler(HichBuilder hircBuilder, BnkHeaderBuilder headerBuilder)
        {
            _hircBuilder = hircBuilder;
            _headerBuilder = headerBuilder;
        }

        public Result<CompileResult> CompileProject(CompilerData audioProject)
        {
            // Build the wwise object graph 
            var header = _headerBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);

            //Ensure all write ids are not causing conflicts. Todo, this will cause issues with reuse of sounds
            var allIds = hircChunk.Hircs.Select(x => x.Id).ToList();
            var originalCount = allIds.Count();
            var uniqueCount = allIds.Distinct().Count();
            //Guard.IsEqualTo(originalCount, uniqueCount);

            var compileResult = new CompileResult()
            {
                Project = audioProject,
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName),
                OutputDatFile = BuildDat(audioProject),
                NameList = null,
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
    }
}
