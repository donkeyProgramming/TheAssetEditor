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


        public bool Load(string packFileSystemPath, bool suppresEvents = false) 
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

                caPackFileContainer.Sort();
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

        public void AddEmptyFolder(PackFileDirectory parent, string name)
        {
            var newFolder = new PackFileDirectory(name);
            parent.AddChild(newFolder);
            parent.Sort();
            Database.TriggerFileAdded(newFolder, parent);
        }

        public void Unload() { }
        public void Save() {  }
        public void FindFile() { }

        public void NewPackFile(string name) 
        {
            var newPackFile = new PackFileContainer(name)
            { 
                Header = new PFHeader("PFH5")
            };
            Database.AddPackFile(newPackFile);
        }

        public void RenameFile(IPackFile packFile, string newName)
        {
            packFile.Name = newName;
        }

        public void AddFileToPack(PackFileDirectory parent, IPackFile newFile)
        {
            parent.AddChild(newFile);
            parent.Sort();
            Database.TriggerFileAdded(newFile, parent);
        }

        public void AddFolderContent(IPackFile parent, string folderDir)
        {
            var originalFilePaths = Directory.GetFiles(folderDir, "*", SearchOption.AllDirectories);
            var filePaths = originalFilePaths.Select(x => x.Replace(folderDir+"\\", "")).ToList();

            for (int i = 0; i < filePaths.Count; i++)
            {
                var currentPath = filePaths[i];
                var filename = Path.GetFileName(currentPath);
                var dirPath = Path.GetDirectoryName(currentPath);
                var directory = GetDirectory(parent, dirPath);
                var source = MemorySource.FromFile(originalFilePaths[i]);

                var file = new PackFile(filename, "AddFolderContent does not set full path", source);
                file.Parent = directory;
                directory.AddChild(file);
            }

            parent.Sort();
            Database.TriggerFileAdded(parent, parent);
        }


        IPackFile GetDirectory(IPackFile parent, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return parent;
            var dirPaths = name.Split('\\');
            return CreatFolder(parent, dirPaths, 0);
        }

        IPackFile CreatFolder(IPackFile pack, string[] directoryNames, int index)
        {
            if (index == directoryNames.Length - 1)
                return pack;
            foreach (var child in pack.Children)
            {
                if (child.PackFileType() == PackFileType.Directory)
                {
                    if (child.Name == directoryNames[index])
                        return CreatFolder(child, directoryNames, index + 1);
                }
            }

            var newFolder = new PackFileDirectory(directoryNames[index]);
            pack.AddChild(newFolder);
            CreatFolder(newFolder, directoryNames, index + 1);
            return newFolder;
        }


        public PackFileContainer GetRoot(IPackFile item)
        {
            var root = item;
            while ((root = item.Parent) != null)
            {


            }

            return root as PackFileContainer;
        }
    }
}
