using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Storage
{
    public class BnkLoader
    {
        public class LoadResult
        {
            public Dictionary<uint, List<HircItem>> HircList { get; internal set; } = [];
            public Dictionary<uint, List<DidxAudio>> DidxAudioList { get; internal set; } = [];
            public Dictionary<string, PackFile> PackFileMap { get; internal set; } = [];
        }

        private readonly IPackFileService _packFileService;
        private readonly BnkParser _bnkParser;
        readonly ILogger _logger = Logging.Create<BnkLoader>();

        public BnkLoader(IPackFileService packFileService, BnkParser bnkParser)
        {
            _packFileService = packFileService;
            _bnkParser = bnkParser;
        }

        public ParsedBnkFile LoadBnkFile(PackFile bnkFile, string bnkFileName, bool isCaHircItem, bool printData = false)
        {
            var soundDb = _bnkParser.Parse(bnkFile, bnkFileName, isCaHircItem);
            if (printData)
                PrintHircList(soundDb.HircChuck.Hircs, bnkFileName);
            return soundDb;
        }

        public LoadResult LoadBnkFiles(bool onlyEnglish = true)
        {
            var bankFiles = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".bnk");
            var bankFilesAsDictionary = bankFiles.GroupBy(f => f.FileName).ToDictionary(g => g.Key, g => g.Last().Pack);
            var removeFilter = new List<string>() { "media", "init.bnk", "animation_blood_data.bnk" };
            if (onlyEnglish)
                removeFilter.AddRange(new List<string>() { "chinese", "french(france)", "german", "italian", "polish", "russian", "spanish(spain)" });

            var wantedBnkFiles = PackFileUtil.FilterUnvantedFiles(bankFilesAsDictionary, removeFilter.ToArray(), out var removedFiles); ;
            _logger.Here().Information($"Parsing game sounds. {bankFiles.Count} bnk files found. {wantedBnkFiles.Count} after filtering");

            var parsedBnkList = new List<ParsedBnkFile>();
            var banksWithUnknowns = new List<string>();
            var failedBnks = new List<(string bnkFile, string Error)>();

            var counter = 1;

            var output = new LoadResult();

            Parallel.ForEach(wantedBnkFiles, bnkFile =>
            {
                var name = bnkFile.Key;
                var file = bnkFile.Value;
                var filePack = _packFileService.GetPackFileContainer(file);
                _logger.Here().Information($"{counter++}/{wantedBnkFiles.Count} - {name}");
                output.PackFileMap.Add(file.Name, file);

                try
                {
                    var parsedBnk = LoadBnkFile(file, name, filePack.IsCaPackFile);
                    if (parsedBnk.HircChuck.Hircs.Any(y => y is CAkUnknown == true || y.HasError))
                        banksWithUnknowns.Add(name);

                    parsedBnkList.Add(parsedBnk);
                }
                catch (Exception e)
                {
                    failedBnks.Add((name, e.Message));
                }
            });

            // Combine the data
            foreach (var parsedBnk in parsedBnkList)
            {
                // Build Audio Hircs from DIDX and DATA
                if (parsedBnk.DataChunk is not null && parsedBnk.DidxChunk is not null)
                {
                    foreach (var didx in parsedBnk.DidxChunk.MediaList)
                    {
                        var didxAudio = new DidxAudio()
                        {
                            Id = didx.Id,
                            ByteArray = parsedBnk.DataChunk.GetBytesFromBuffer((int)didx.Offset, (int)didx.Size),
                            OwnerFile = parsedBnk.Header.OwnerFileName,
                        };

                        if (output.DidxAudioList.ContainsKey(didx.Id) is false)
                            output.DidxAudioList[didx.Id] = new List<DidxAudio>();
                        output.DidxAudioList[didx.Id].Add(didxAudio);
                    }
                }

                foreach (var item in parsedBnk.HircChuck.Hircs)
                {
                    if (output.HircList.ContainsKey(item.Id) == false)
                        output.HircList[item.Id] = new List<HircItem>();

                    output.HircList[item.Id].Add(item);
                }
            }

            // Print it all
            var allHircs = parsedBnkList.SelectMany(x => x.HircChuck.Hircs);
            PrintHircList(allHircs, "All");

            if (failedBnks.Count != 0)
                _logger.Here().Error($"{failedBnks.Count} banks failed: {string.Join("\n", failedBnks)}");

            return output;
        }

        void PrintHircList(IEnumerable<HircItem> hircItems, string header)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"\n Result: {header}");
            var unknownHirc = hircItems.Where(hircItem => hircItem is CAkUnknown).Count();
            var errorHirc = hircItems.Where(hircItem => hircItem.HasError).Count();
            stringBuilder.AppendLine($"\t Total HircObjects: {hircItems.Count()} Unknown: {unknownHirc} Decoding Errors:{errorHirc}");

            var grouped = hircItems.GroupBy(hircItem => hircItem.Type);
            var groupedWithError = grouped.Where(groupedHircItems => groupedHircItems.Any(y => y is CAkUnknown == true || y.HasError));
            var groupedWithoutError = grouped.Where(groupedHircItems => groupedHircItems.Any(y => y is CAkUnknown == false && y.HasError == false));

            stringBuilder.AppendLine("\t\t Correct:");
            foreach (var group in groupedWithoutError)
                stringBuilder.AppendLine($"\t\t\t {group.Key}: Count: {group.Count()}");

            if (groupedWithError.Any())
            {
                stringBuilder.AppendLine("\t\t Error:");
                foreach (var group in groupedWithError)
                    stringBuilder.AppendLine($"\t\t\t {group.Key}: {group.Where(x => x is CAkUnknown == true || x.HasError).Count()}/{group.Count()} Failed");
            }

            _logger.Here().Information(stringBuilder.ToString());
        }
    }
}
