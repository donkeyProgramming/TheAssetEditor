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

namespace Editors.Audio.Shared.Storage
{
    public class BnkLoader(IPackFileService packFileService)
    {
        public class Result
        {
            public Dictionary<uint, List<HircItem>> HircsById { get; internal set; } = [];
            public Dictionary<uint, List<DidxAudio>> DidxAudioListById { get; internal set; } = [];
            public Dictionary<string, PackFile> PackFileByBnkName { get; internal set; } = [];
        }

        private readonly IPackFileService _packFileService = packFileService;
        readonly ILogger _logger = Logging.Create<BnkLoader>();

        public ParsedBnkFile LoadBnkFile(PackFile bnkFile, string bnkFilePath, bool isCAHircItem, bool printData = false)
        {
            var soundDb = BnkParser.Parse(bnkFile, bnkFilePath, isCAHircItem);
            if (printData)
                PrintHircList(soundDb.HircChunk.HircItems, bnkFilePath);
            return soundDb;
        }

        public Result LoadBnkFiles(List<string> languageToFilterOut)
        {
            var bankFiles = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".bnk");
            var bankFilesAsDictionary = bankFiles.GroupBy(f => f.FileName).ToDictionary(g => g.Key, g => g.Last().Pack);

            var removeFilter = new List<string>() { "media", "init.bnk", "animation_blood_data.bnk" };
            removeFilter.AddRange(languageToFilterOut);

            var wantedBnkFiles = PackFileUtil.FilterUnvantedFiles(bankFilesAsDictionary, removeFilter.ToArray(), out var removedFiles); ;
            _logger.Here().Information($"Parsing game sounds. {bankFiles.Count} bnk files found. {wantedBnkFiles.Count} after filtering");

            var parsedBnks = new List<ParsedBnkFile>();
            var bnksWithUnknownHircs = new List<string>();
            var failedBnks = new List<(string bnkFile, string Error)>();
            var result = new Result();
            var counter = 1;

            Parallel.ForEach(wantedBnkFiles, bnkFile =>
            {
                var filePath = bnkFile.Key;
                _logger.Here().Information($"{counter++}/{wantedBnkFiles.Count} - {filePath}");

                var packFile = bnkFile.Value;
                var packFileContainer = _packFileService.GetPackFileContainer(packFile);
                result.PackFileByBnkName.TryAdd(packFile.Name, packFile);

                try
                {
                    var parsedBnk = LoadBnkFile(packFile, filePath, packFileContainer.IsCaPackFile);
                    if (parsedBnk.HircChunk.HircItems.Any(hicItem => hicItem is UnknownHircItem == true || hicItem.HasError))
                        bnksWithUnknownHircs.Add(filePath);

                    parsedBnks.Add(parsedBnk);
                }
                catch (Exception e)
                {
                    failedBnks.Add((filePath, e.Message));
                }
            });

            var allHircItems = parsedBnks.SelectMany(x => x.HircChunk.HircItems);
            PrintHircList(allHircItems, "All");
            if (failedBnks.Count != 0)
                _logger.Here().Error($"{failedBnks.Count} banks failed: {string.Join("\n", failedBnks)}");

            result.HircsById = parsedBnks
                .Where(parsedBnk => parsedBnk.HircChunk is not null)
                .SelectMany(parsedBnk => parsedBnk.HircChunk.HircItems)
                .GroupBy(item => item.Id)
                .ToDictionary(group => group.Key, group => group.ToList());


            result.DidxAudioListById = parsedBnks
                .Where(parsedBnk => parsedBnk.DataChunk is not null && parsedBnk.DidxChunk is not null)
                .SelectMany(parsedBnk =>
                    parsedBnk.DidxChunk.MediaList.Select(didx => new DidxAudio()
                    {
                        Id = didx.Id,
                        ByteArray = parsedBnk.DataChunk.GetBytesFromBuffer((int)didx.Offset, (int)didx.Size),
                        OwnerFilePath = parsedBnk.BkhdChunk.OwnerFilePath,
                        LanguageId = parsedBnk.BkhdChunk.AkBankHeader.LanguageId
                    }))
                .GroupBy(didxAudio => didxAudio.Id)
                .ToDictionary(group => group.Key, group => group.ToList());

            return result;
        }

        void PrintHircList(IEnumerable<HircItem> hircItems, string header)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"\n Result: {header}");
            var unknownHirc = hircItems.Where(hircItem => hircItem is UnknownHircItem).Count();
            var errorHirc = hircItems.Where(hircItem => hircItem.HasError).Count();
            stringBuilder.AppendLine($"\t Total Hirc Items: {hircItems.Count()} Unknown: {unknownHirc} Decoding Errors:{errorHirc}");

            var grouped = hircItems.GroupBy(hircItem => hircItem.HircType);
            var groupedWithError = grouped.Where(groupedHircItems => groupedHircItems.Any(y => y is UnknownHircItem == true || y.HasError));
            var groupedWithoutError = grouped.Where(groupedHircItems => groupedHircItems.Any(y => y is UnknownHircItem == false && y.HasError == false));

            stringBuilder.AppendLine("\t\t Succeeded:");
            foreach (var group in groupedWithoutError)
                stringBuilder.AppendLine($"\t\t\t {group.Key}: Count: {group.Count()}");

            if (groupedWithError.Any())
            {
                stringBuilder.AppendLine("\t\t Failed:");
                foreach (var group in groupedWithError)
                    stringBuilder.AppendLine($"\t\t\t {group.Key}: {group.Where(x => x is UnknownHircItem == true || x.HasError).Count()}/{group.Count()} Failed");
            }

            _logger.Here().Information(stringBuilder.ToString());
        }
    }
}
