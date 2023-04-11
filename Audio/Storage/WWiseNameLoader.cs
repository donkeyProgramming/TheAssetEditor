using Audio.FileFormats;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.Storage
{
    public class WWiseNameLoader
    {
        private readonly PackFileService _pfs;

        private Dictionary<uint, string> _nameLookUp { get; set; } = new Dictionary<uint, string>();


        public WWiseNameLoader(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public Dictionary<uint, string> BuildNameHelper()
        {
            var wh3Db = LoadWhDatDbForWh3(_pfs, out var _);
            var wh3DbNameList = wh3Db.CreateFileNameList();
            AddNames(wh3DbNameList);

            // Add all the bnk file names 
            var bnkFiles = _pfs.FindAllWithExtention(".bnk");
            var bnkNames = bnkFiles.Select(x => x.Name.Replace(".bnk", "")).ToArray();
            AddNames(bnkNames);

            // Load all string from the game exe
            if (File.Exists(@"C:\Users\ole_k\Desktop\Strings\game_wh3_1_2.txt"))
            {
                var exeContent = File.ReadAllLines(@"C:\Users\ole_k\Desktop\Strings\game_wh3_1_2.txt");
                exeContent = exeContent.Select(x => x.ToLower()).ToArray();
                var exeContentDistinct = exeContent.Distinct().ToArray();
                AddNames(exeContent);
            }

            // Load all from wwiser
            if (File.Exists(@"C:\Users\ole_k\Desktop\Wh3 sounds\wwiser.txt"))
            {
                var filecontent = File.ReadAllLines(@"C:\Users\ole_k\Desktop\Wh3 sounds\wwiser.txt");
                AddNames(filecontent);
            }

            // Load all from game db tables
            if (Directory.Exists(@"C:\Users\ole_k\Desktop\Wh3 sounds\DbTables"))
            {
                var files = Directory.GetFiles(@"C:\Users\ole_k\Desktop\Wh3 sounds\DbTables");
                foreach (var file in files)
                {
                    var fileLines = File.ReadAllLines(file);
                    foreach (var fileLine in fileLines)
                    {
                        var content = fileLine.Split("\t");
                        AddNames(content);
                    }
                }
            }

            return _nameLookUp;
        }

        // Move to a datfile loader
        SoundDatFile LoadWhDatDbForWh3(PackFileService pfs, out List<string> failedFiles)
        {
            var datFiles = pfs.FindAllWithExtention(".dat");
            datFiles = PackFileUtil.FilterUnvantedFiles(pfs, datFiles, new[] { "bank_splits.dat", "campaign_music.dat", "battle_music.dat", "icudt61l.dat" }, out var removedFiles);

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
                    var fullPath = pfs.GetFullPath(datFile);
                    failedDatParsing.Add((fullPath, e.Message));
                }
            }

            failedFiles = failedDatParsing.Select(x => x.Item1).ToList();
            return masterDat;
        }

        void AddNames(string[] names)
        {
            foreach (var name in names)
            {
                var hashVal = WWiseHash.ComputeHash(name);
                _nameLookUp[hashVal] = name;
            }
        }
    }

}
