using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Didx;
using Shared.GameFormats.WWise.Hirc;
using Shared.GameFormats.WWise.Hirc.V136;

namespace Editors.Audio.Storage
{
    public class BnkLoader
    {
        protected readonly IAudioRepository _repository;
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }

        public class LoadResult
        {
            public Dictionary<uint, List<HircItem>> HircList { get; internal set; } = new();
            public Dictionary<uint, List<DidxAudio>> DidxAudioList { get; internal set; } = new();
        }

        private readonly IPackFileService _pfs;
        private readonly BnkParser _bnkParser;
        readonly ILogger _logger = Logging.Create<BnkLoader>();

        public BnkLoader(IPackFileService pfs, BnkParser bnkParser)
        {
            _pfs = pfs;
            _bnkParser = bnkParser;
        }

        public ParsedBnkFile LoadBnkFile(PackFile bnkFile, string bnkFileName, bool printData = false)
        {
            var soundDb = _bnkParser.Parse(bnkFile, bnkFileName);
            if (printData)
                PrintHircList(soundDb.HircChuck.Hircs, bnkFileName);
            return soundDb;
        }

        public LoadResult LoadBnkFiles(bool onlyEnglish = true)
        {
            var bankFiles = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_pfs, ".bnk");
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

            Parallel.ForEach(wantedBnkFiles, bnkFile =>
            {
                var name = bnkFile.Key;
                var file = bnkFile.Value;
                _logger.Here().Information($"{counter++}/{wantedBnkFiles.Count} - {name}");

                try
                {
                    var parsedBnk = LoadBnkFile(file, name);
                    if (parsedBnk.HircChuck.Hircs.Count(y => y is CAkUnknown == true || y.HasError) != 0)
                        banksWithUnknowns.Add(name);

                    parsedBnkList.Add(parsedBnk);
                }
                catch (Exception e)
                {
                    failedBnks.Add((name, e.Message));
                }
            });

            var output = new LoadResult();

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

            if (failedBnks.Any())
                _logger.Here().Error($"{failedBnks.Count} banks failed: {string.Join("\n", failedBnks)}");

            return output;
        }

        void PrintHircList(IEnumerable<HircItem> hircItems, string header)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"\n Result: {header}");
            var unknownHirc = hircItems.Where(X => X is CAkUnknown).Count();
            var errorHirc = hircItems.Where(X => X.HasError).Count();
            stringBuilder.AppendLine($"\t Total HircObjects: {hircItems.Count()} Unknown: {unknownHirc} Decoding Errors:{errorHirc}");

            var grouped = hircItems.GroupBy(x => x.Type);
            var groupedWithError = grouped.Where(x => x.Count(y => y is CAkUnknown == true || y.HasError) != 0);
            var groupedWithoutError = grouped.Where(x => x.Count(y => y is CAkUnknown == false && y.HasError == false) != 0);

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
