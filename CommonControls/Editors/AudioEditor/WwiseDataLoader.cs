using CommonControls.Common;
using CommonControls.Editors.Sound;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public interface IBnkProvider
    {
        List<PackFile> GetBnkFiles();
        string GetFullName(PackFile pf);
    }

    public class PackFileBnkProvider : IBnkProvider
    {
        private readonly PackFileService _pfs;

        public PackFileBnkProvider(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public List<PackFile> GetBnkFiles() => _pfs.FindAllWithExtention(".bnk");
        public string GetFullName(PackFile pf) => _pfs.GetFullPath(pf);
    }


    public class WwiseDataLoader
    {
        ILogger _logger = Logging.Create<WwiseDataLoader>();

        public ExtenededSoundDataBase BuildMasterSoundDatabase(List<SoundDataBase> soundDatabases )
        {
            ExtenededSoundDataBase masterDb = new ExtenededSoundDataBase();
            foreach (var db in soundDatabases)
                masterDb.AddHircItems(db.Hircs);
            return masterDb;
        }

        public List<string> LoadPotentialWWiseSounds()
        {
            return new List<string>();
        }

        public SoundDatFile LoadWhDatDbForWh3(PackFileService pfs, out List<string> failedFiles)
        {
            var datFiles = pfs.FindAllWithExtention(".dat");
            datFiles = PackFileUtil.FilterUnvantedFiles(datFiles, new[] { "bank_splits.dat", "campaign_music.dat", "battle_music.dat" }, out var removedFiles);

            var failedDatParsing = new List<(string, string)>();
            var masterDat = new SoundDatFile();
            foreach (var datFile in datFiles)
            {
                try
                {
                    var parsedFile = DatParser.Parse(datFile, false);
                    masterDat.Merge(parsedFile);
                }
                catch (Exception e)
                {
                    failedDatParsing.Add((datFile.Name, e.Message));
                }
            }

            failedFiles = failedDatParsing.Select(x => x.Item1).ToList();
            return masterDat;
        }


        public List<SoundDataBase> LoadBnkFiles(IBnkProvider fileProvider)
        {
            var bankFiles = fileProvider.GetBnkFiles();
            var wantedBnkFiles = PackFileUtil.FilterUnvantedFiles(bankFiles, new[] { "media", "init.bnk", "animation_blood_data.bnk" }, out var removedFiles);

            _logger.Here().Information($"Parsing game sounds. {bankFiles.Count} bnk files found. {wantedBnkFiles.Count} after filtering");

            var globalSoundDatabase = new Dictionary<string, SoundDataBase>();
            var banksWithUnknowns = new List<string>();
            var failedBnks = new List<(string bnkFile, string Error)>();

            var counter = 1;
            foreach (var bnkFile in wantedBnkFiles)
            {
                var name = fileProvider.GetFullName(bnkFile);
                _logger.Here().Information($"{counter++}/{wantedBnkFiles.Count()} - {name}");

                try
                {
                    var soundDb = Bnkparser.Parse(bnkFile);
                    PrintHircList(soundDb.Hircs, name);

                    if (soundDb.Hircs.Count(y => (y is CAkUnknown) == true || y.HasError) != 0)
                        banksWithUnknowns.Add(name);

                    globalSoundDatabase.Add(name, soundDb);
                }
                catch (Exception e)
                {
                    failedBnks.Add((name, e.Message));
                }
            }

            if (banksWithUnknowns.Any())
                _logger.Here().Warning($"{banksWithUnknowns.Count} banks contains unknown info : {string.Join("\n", banksWithUnknowns)}");

            if (failedBnks.Any())
                _logger.Here().Error($"{failedBnks.Count} banks failed: {string.Join("\n", failedBnks)}");

            var allHircs = globalSoundDatabase.SelectMany(x => x.Value.Hircs);
            PrintHircList(allHircs, "All");

            return globalSoundDatabase.Select(x => x.Value).ToList();
        }

        void PrintHircList(IEnumerable<HircItem> hircItems, string header)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"\n Result: {header}");
            var unknownHirc = hircItems.Where(X => X is CAkUnknown).Count();
            var errorHirc = hircItems.Where(X => X.HasError).Count();
            stringBuilder.AppendLine($"\t Total HircObjects: {hircItems.Count()} Unknown: {unknownHirc} Decoding Errors:{errorHirc}");

            var grouped = hircItems.GroupBy(x => x.Type);
            var groupedWithError = grouped.Where(x => x.Count(y => (y is CAkUnknown) == true || y.HasError) != 0);
            var groupedWithoutError = grouped.Where(x => x.Count(y => (y is CAkUnknown) == false && y.HasError == false) != 0);

            stringBuilder.AppendLine("\t\t Correct:");
            foreach (var group in groupedWithoutError)
                stringBuilder.AppendLine($"\t\t\t {group.Key}: Count: {group.Count()}");

            if (groupedWithError.Any())
            {
                stringBuilder.AppendLine("\t\t Error:");
                foreach (var group in groupedWithError)
                    stringBuilder.AppendLine($"\t\t\t {group.Key}: {group.Where(x => (x is CAkUnknown) == true || x.HasError).Count()}/{group.Count()} Failed");
            }

            _logger.Here().Information(stringBuilder.ToString());
        }
    }
}
