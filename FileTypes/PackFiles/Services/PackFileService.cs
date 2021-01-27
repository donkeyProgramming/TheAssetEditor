using Common;
using FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypes.PackFiles.Services
{
    public class PackFileService
    {
        ILogger _logger = Logging.Create<PackFileService>();
        
        
        public PackFileDataBase Database { get; private set; }

        public PackFileService(PackFileDataBase database)
        {
            Database = database;
        }


        public bool Load(string packFileSystemPath) 
        {
            try
            {
                if (!File.Exists(packFileSystemPath))
                {
                    _logger.Here().Error($"Trying to load file {packFileSystemPath}, which can not be located");
                    return false;
                }

                using (var fileStram = File.OpenRead(packFileSystemPath))
                {
                    using (var reader = new BinaryReader(fileStram, Encoding.ASCII))
                    {
                        var pack = new PackFileContainer(packFileSystemPath, reader);
                        Database.AddPackFile(pack);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Trying to load file {packFileSystemPath}. Error : {e.ToString()}");
                return false;
            }

            return true;
        }

        public bool LoadAllCaFiles(string gameDataFolder)
        {
            var allCaPackFiles = GetPackFilesFromManifest(gameDataFolder);
            var packList = new List<PackFileContainer>();
            foreach (var packFilePath in allCaPackFiles)
            {
                var path = gameDataFolder + "\\" + packFilePath;
                using (var fileStram = File.OpenRead(path))
                {
                    using (var reader = new BinaryReader(fileStram, Encoding.ASCII))
                    {
                        var pack = new PackFileContainer(path, reader);
                        packList.Add(pack);
                    }
                }
            }

            PackFileContainer caPackFileContainer = new PackFileContainer("All CA packs");
            var packFilesOrderedByGroup = packList
                .GroupBy(x => x.Header.LoadOrder)
                .OrderBy(x => x.Key);

            foreach (var group in packFilesOrderedByGroup)
            {
                var packFilesOrderedByName = group.OrderBy(x => x.Name);
                foreach (var packfile in packFilesOrderedByName)
                    caPackFileContainer.MergePackFileContainer(packfile);
            }

            Database.AddPackFile(caPackFileContainer);
            caPackFileContainer.Sort();

            return true;
        }



        List<string> GetPackFilesFromManifest(string gameDataFolder)
        {
            var output = new List<string>();
            var lines = File.ReadAllLines(gameDataFolder + "\\manifest.txt");
            foreach (var line in lines)
            {
                var items = line.Split('\t');
                if (items[0].Contains(".pack"))
                {
                    output.Add(items[0].Trim());
                }

            }
            return output;
        }



        public void Unload() { }
        public void Save() {  }
        public void FindFile() { }
    }
}
