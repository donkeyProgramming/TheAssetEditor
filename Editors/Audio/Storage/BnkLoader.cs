using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Utility;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Storage
{
    // TODO: Add a bnk file hirc lookup
    public class BnkLoader
    {
        public class LoadResult
        {
            public Dictionary<uint, Dictionary<uint, List<HircItem>>> HircLookupByLanguageIDByID { get; internal set; } = [];
            public Dictionary<uint, Dictionary<uint, List<ICAkSound>>> SoundHircLookupByLanguageIDBySourceID { get; internal set; } = [];
            public Dictionary<uint, Dictionary<uint, List<DidxAudio>>> DidxAudioLookupByLanguageIDByID { get; internal set; } = [];
            public Dictionary<uint, List<HircItem>> HircLookupByID { get; internal set; } = [];
            public Dictionary<uint, List<DidxAudio>> DidxAudioLookupByID { get; internal set; } = [];
            public Dictionary<string, PackFile> BnkPackFileLookupByName { get; internal set; } = [];
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
                PrintHircList(soundDb.HircChunk.HircItems, bnkFileName);
            return soundDb;
        }

        public LoadResult LoadBnkFiles()
        {
            var bankFiles = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".bnk");
            var bankFilesAsDictionary = bankFiles.GroupBy(f => f.FileName).ToDictionary(g => g.Key, g => g.Last().Pack);

            var removeFilter = new List<string>() { "media", "init.bnk", "animation_blood_data.bnk" };
            var languages = new List<string>() { "chinese", "french(france)", "german", "italian", "polish", "russian", "spanish(spain)" };
            //removeFilter.AddRange(languages);

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
                output.BnkPackFileLookupByName.TryAdd(file.Name, file);

                try
                {
                    var parsedBnk = LoadBnkFile(file, name, filePack.IsCaPackFile);
                    if (parsedBnk.HircChunk.HircItems.Any(y => y is UnknownHirc == true || y.HasError))
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
                // Build DIDX Audio Items from DIDX and DATA
                if (parsedBnk.DataChunk is not null && parsedBnk.DidxChunk is not null)
                {
                    foreach (var didx in parsedBnk.DidxChunk.MediaList)
                    {
                        var didxAudio = new DidxAudio()
                        {
                            ID = didx.ID,
                            ByteArray = parsedBnk.DataChunk.GetBytesFromBuffer((int)didx.Offset, (int)didx.Size),
                            OwnerFilePath = parsedBnk.BkhdChunk.OwnerFilePath,
                            LanguageID = parsedBnk.BkhdChunk.AkBankHeader.LanguageID
                        };

                        if (output.DidxAudioLookupByID.ContainsKey(didx.ID) is false)
                            output.DidxAudioLookupByID[didx.ID] = new List<DidxAudio>();
                        output.DidxAudioLookupByID[didx.ID].Add(didxAudio);
                    }
                }

                foreach (var item in parsedBnk.HircChunk.HircItems)
                {
                    if (output.HircLookupByID.ContainsKey(item.ID) == false)
                        output.HircLookupByID[item.ID] = new List<HircItem>();

                    output.HircLookupByID[item.ID].Add(item);
                }
            }

            // Print it all
            var allHircItems = parsedBnkList.SelectMany(x => x.HircChunk.HircItems);
            PrintHircList(allHircItems, "All");

            if (failedBnks.Count != 0)
                _logger.Here().Error($"{failedBnks.Count} banks failed: {string.Join("\n", failedBnks)}");

            // Construct language based Hirc Item data
            output.HircLookupByLanguageIDByID = output.HircLookupByID
                .SelectMany(kvp => kvp.Value)
                .GroupBy(item => item.LanguageID)
                .ToDictionary(
                    langGroup => langGroup.Key,
                    langGroup => langGroup
                        .GroupBy(item => item.ID)
                        .ToDictionary(
                            idGroup => idGroup.Key,
                            idGroup => idGroup.ToList()
                        )
                );

            // Construct language Sound Source ID data
            output.SoundHircLookupByLanguageIDBySourceID = output.HircLookupByLanguageIDByID.ToDictionary(
                language => language.Key,
                language => language.Value.Values
                    .SelectMany(itemList => itemList)
                    .Where(hircItem => hircItem is ICAkSound)
                    .Cast<ICAkSound>()
                    .GroupBy(sound => sound.GetSourceID())
                    .ToDictionary(
                        sourceGroup => sourceGroup.Key,
                        sourceGroup => sourceGroup.ToList()
                    )
            );

            // Construct language DIDX Audio ID
            output.DidxAudioLookupByLanguageIDByID = output.DidxAudioLookupByID
                .SelectMany(kvp => kvp.Value)
                .GroupBy(item => item.LanguageID)
                .ToDictionary(
                    langGroup => langGroup.Key,
                    langGroup => langGroup
                        .GroupBy(item => item.ID)
                        .ToDictionary(
                            idGroup => idGroup.Key,
                            idGroup => idGroup.ToList()
                        )
                );

            // TODO: Temporary solution to limit what the Audio Explorer uses to english language stuff before I rework the audio explorer
            foreach (var id in output.HircLookupByID)
                id.Value.RemoveAll(item => languages.Any(language => item.OwnerFilePath.Contains(language)));

            return output;
        }

        void PrintHircList(IEnumerable<HircItem> hircItems, string header)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"\n Result: {header}");
            var unknownHirc = hircItems.Where(hircItem => hircItem is UnknownHirc).Count();
            var errorHirc = hircItems.Where(hircItem => hircItem.HasError).Count();
            stringBuilder.AppendLine($"\t Total Hirc Items: {hircItems.Count()} Unknown: {unknownHirc} Decoding Errors:{errorHirc}");

            var grouped = hircItems.GroupBy(hircItem => hircItem.HircType);
            var groupedWithError = grouped.Where(groupedHircItems => groupedHircItems.Any(y => y is UnknownHirc == true || y.HasError));
            var groupedWithoutError = grouped.Where(groupedHircItems => groupedHircItems.Any(y => y is UnknownHirc == false && y.HasError == false));

            stringBuilder.AppendLine("\t\t Succeeded:");
            foreach (var group in groupedWithoutError)
                stringBuilder.AppendLine($"\t\t\t {group.Key}: Count: {group.Count()}");

            if (groupedWithError.Any())
            {
                stringBuilder.AppendLine("\t\t Failed:");
                foreach (var group in groupedWithError)
                    stringBuilder.AppendLine($"\t\t\t {group.Key}: {group.Where(x => x is UnknownHirc == true || x.HasError).Count()}/{group.Count()} Failed");
            }

            _logger.Here().Information(stringBuilder.ToString());
        }
    }
}
