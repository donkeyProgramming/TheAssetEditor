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


        public PackFileContainer Load(string packFileSystemPath) 
        {
            try
            {
                if (!File.Exists(packFileSystemPath))
                {
                    _logger.Here().Error($"Trying to load file {packFileSystemPath}, which can not be located");
                    return null;
                }

                using (var fileStram = File.OpenRead(packFileSystemPath))
                {
                    using (var reader = new BinaryReader(fileStram, Encoding.ASCII))
                    {
                        return Load(reader, packFileSystemPath);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Trying to load file {packFileSystemPath}. Error : {e}");
                return null;
            }
        }

        public List<PackFile> FindAllWithExtention(string extention)
        {
            extention = extention.ToLower();
            List<PackFile> output = new List<PackFile>();
            foreach (var pf in Database.PackFiles)
            {
                foreach (var file in pf.FileList)
                {
                    var fileExtention = Path.GetExtension(file.Key);
                    if(fileExtention == extention)
                        output.Add(file.Value as PackFile);
                }
            }

            return output;
        }

        public List<PackFile> FindAllFilesInDirectory(string dir)
        {
            dir = dir.ToLower();
            List<PackFile> output = new List<PackFile>();
            foreach (var pf in Database.PackFiles)
            {
                foreach (var file in pf.FileList)
                {
                    if(file.Key.IndexOf(dir) == 0)
                        output.Add(file.Value as PackFile);
                }
            }

            return output;
        }

        public string GetFullPath(PackFile file)
        {
            foreach (var pf in Database.PackFiles)
            {
                var res = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
                if (string.IsNullOrWhiteSpace(res) == false)
                    return res;
            }
            throw new Exception("Unknown path for " + file.Name);
        }

        public PackFileContainer Load(BinaryReader binaryReader, string packFileSystemPath)
        {
            var pack = new PackFileContainer(packFileSystemPath, binaryReader);
            Database.AddPackFile(pack);
            return pack;
        }

        public bool LoadAllCaFiles(string gameDataFolder)
        {
            try
            {
                _logger.Here().Information($"Loading all ca packfiles located in {gameDataFolder}");
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
                caPackFileContainer.IsCaPackFile = true;
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
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Trying to all ca packs in {gameDataFolder}. Error : {e.ToString()}");
                return false;
            }

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
                    output.Add(items[0].Trim());
            }
            return output;
        }

        // Add
        // ---------------------------
        public PackFileContainer CreateNewPackFile(string name, PackFileCAType type)
        {
            var newPackFile = new PackFileContainer(name)
            {
                Header = new PFHeader("PFH5", type),
                
            };
            Database.AddPackFile(newPackFile);
            return newPackFile;
        }


        public void AddFileToPack(PackFileContainer container, string path, IPackFile newFile)
        {
            if (!string.IsNullOrWhiteSpace(path))
                path += "\\";
            path += newFile.Name;
            container.FileList[path.ToLower()] = newFile;
            Database.TriggerContainerUpdated(container);
        }

        public void AddFolderContent(PackFileContainer container, string path, string folderDir)
        {
            var originalFilePaths = Directory.GetFiles(folderDir, "*", SearchOption.AllDirectories);
            var filePaths = originalFilePaths.Select(x => x.Replace(folderDir + "\\", "")).ToList();
            if (!string.IsNullOrWhiteSpace(path))
                path += "\\";
            for (int i = 0; i < filePaths.Count; i++)
            {
                var currentPath = filePaths[i];
                var filename = Path.GetFileName(currentPath);

                var source = MemorySource.FromFile(originalFilePaths[i]);
                var file = new PackFile(filename, source);

                container.FileList[path.ToLower() + currentPath.ToLower()] = file;
            }

            Database.TriggerContainerUpdated(container);
        }

        // Remove
        // ---------------------------
        public void UnloadPackContainer(PackFileContainer pf)
        {
            Database.RemovePackFile(pf);
        }

        public void DeleteFolder(PackFileContainer pf, string folder)
        {
            var folderLower = folder.ToLower();
            var itemsToDelete = pf.FileList.Where(x => x.Key.StartsWith(folderLower));
            foreach (var item in itemsToDelete)
            {
                _logger.Here().Information($"Deleting file {item.Key} in directory {folder}");
                pf.FileList.Remove(item.Key);
            }

            Database.TriggerContainerUpdated(pf);
        }

        public void DeleteFile(PackFileContainer pf, IPackFile file)
        {
            var key = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
            _logger.Here().Information($"Deleting file {key}");
            pf.FileList.Remove(key);
        }

        // Modify
        // ---------------------------
        public void RenameFile(PackFileContainer pf, IPackFile file, string newName)
        {
            var key = pf.FileList.FirstOrDefault(x => x.Value == file).Key;
            pf.FileList.Remove(key);

            var dir = Path.GetDirectoryName(key);
            file.Name = newName;
            pf.FileList[dir + "\\" + file.Name] = file;

            Database.TriggerContainerUpdated(pf);
        }


        public void Save(PackFileContainer pf, BinaryWriter writer)
        {
            pf.SaveToByteArray(writer);
            // Compute the size of all the files
           
        }

        public IPackFile FindFile(string path) 
        {
            var lowerPath = path.Replace('/', '\\').ToLower();
            _logger.Here().Information($"Searching for file {lowerPath}");
            foreach (var packFile in Database.PackFiles)
            {
                if (packFile.FileList.ContainsKey(lowerPath))
                {
                    _logger.Here().Information($"File found");
                    return packFile.FileList[lowerPath];
                }
            }
            _logger.Here().Warning($"File not found");
            return null;
        }
    }
}
