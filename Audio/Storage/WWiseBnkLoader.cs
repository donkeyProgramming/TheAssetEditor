using Audio.FileFormats;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio.Storage
{
    public class WWiseBnkLoader
    {
        private readonly PackFileService _pfs;
        ILogger _logger = Logging.Create<WWiseBnkLoader>();

        public WWiseBnkLoader(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public ParsedBnkFile LoadBnkFile(PackFile bnkFile, string bnkFileName, bool printData = false)
        {
            var soundDb = Bnkparser.Parse(bnkFile, bnkFileName);
            if (printData)
                PrintHircList(soundDb.HircChuck.Hircs, bnkFileName);
            return soundDb;
        }

        public Dictionary<uint, List<HircItem>> LoadBnkFiles(bool onlyEnglish = true)
        {
            var bankFiles = _pfs.FindAllWithExtentionIncludePaths(".bnk");
            var bankFilesAsDictionary = bankFiles.ToDictionary(x => x.FileName, x => x.Pack);
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
                _logger.Here().Information($"{counter++}/{wantedBnkFiles.Count()} - {name}");

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


            // Combine the data
            var mergedHircList = new Dictionary<uint, List<HircItem>>();
            foreach (var parsedBnk in parsedBnkList)
            {
                foreach (var item in parsedBnk.HircChuck.Hircs)
                {
                    if (mergedHircList.ContainsKey(item.Id) == false)
                        mergedHircList[item.Id] = new List<HircItem>();

                    mergedHircList[item.Id].Add(item);
                }
            }

            // Print it all
            var allHircs = parsedBnkList.SelectMany(x => x.HircChuck.Hircs);
            PrintHircList(allHircs, "All");

            // Log errors:
            if (banksWithUnknowns.Any())
                _logger.Here().Warning($"{banksWithUnknowns.Count} banks contains unknown info : {string.Join("\n", banksWithUnknowns)}");

            if (failedBnks.Any())
                _logger.Here().Error($"{failedBnks.Count} banks failed: {string.Join("\n", failedBnks)}");

            return mergedHircList;
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
