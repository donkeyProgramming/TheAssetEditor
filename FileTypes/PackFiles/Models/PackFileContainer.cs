using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypes.PackFiles.Models
{

    public class PackFileContainer : IPackFile
    {
        public string Name { get; set; }

        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;

        Dictionary<string, PackFileDirectory> _directoryMap = new Dictionary<string, PackFileDirectory>();  // used for loading, turn into a variable that is used by the loading code instead

        public IEnumerable<IPackFile> Children => InternalFileList.Values;
        Dictionary<string, IPackFile> InternalFileList { get; set; } = new Dictionary<string, IPackFile>();

        IPackFile IPackFile.Parent { get; set; }

        public PackFileType PackFileType() { return Common.PackFileType.PackContainer; }

        public PackFileContainer(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} - {Header?.LoadOrder}";
        }

        public PackFileContainer(string packFileSystemPath, BinaryReader reader)
        {
            Name = Path.GetFileNameWithoutExtension(packFileSystemPath);
            Header = new PFHeader(reader);

            long sizes = 0;
            long offset = Header.DataStart;
            for (int i = 0; i < Header.FileCount; i++)
            {
                uint size = reader.ReadUInt32();
                sizes += size;
                if (Header.HasAdditionalInfo)
                   reader.ReadUInt32();

                byte isCompressed = 0;
                if (Header.Version == "PFH5")
                    isCompressed = reader.ReadByte();   // For warhammer 2, terrain files are compressed

                string packedFileName = IOFunctions.TheadUnsafeReadZeroTerminatedAscii(reader);

                var packFileName = Path.GetFileName(packedFileName);
                var fileContent = new PackFile(packFileName, packedFileName, new PackedFileSource(packFileSystemPath, offset, size));
                AddFile(fileContent, packedFileName);
                offset += size;
            }
        }

        public void AddFile(IPackFile file, string fullPath)
        {
            var dirName = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(dirName))
            {
                file.Parent = this;
                InternalFileList.Add(file.Name, file);
            }
            else
            {
                // If We dont have the directory, create it
                if (_directoryMap.ContainsKey(dirName) == false)
                {
                    PackFileDirectory parentDir = null;
                    var dirSubPaths = dirName.Split(Path.DirectorySeparatorChar);
                    var currentSubStr = "";
                    for (int i = 0; i < dirSubPaths.Length; i++)
                    {
                        if (currentSubStr.Length != 0)
                            currentSubStr += "\\";
                        currentSubStr += dirSubPaths[i];
                        parentDir = CreateVirtualDirectory(currentSubStr, dirSubPaths[i], parentDir);
                    }
                }

                file.Parent = _directoryMap[dirName];
                _directoryMap[dirName].AddChild(file);
            }
        }

        public void MergePackFileContainer(PackFileContainer other)
        {
            foreach (var file in other.InternalFileList)
                RecursivlyAddFile(file.Value);
        }

        void RecursivlyAddFile(IPackFile file)
        {
            foreach (var child in file.Children)
                RecursivlyAddFile(child);
         
            if (file.PackFileType() == Common.PackFileType.Data)
                AddFile(file, (file as PackFile).FullPath);
        }

        public void Sort()
        {
            InternalFileList = new Dictionary<string, IPackFile>(InternalFileList.OrderBy(x => x.Value.PackFileType()).ThenBy(x => x.Value.Name));
            foreach (var child in Children)
                child.Sort();
        }

        public int FileCount()
        {
            var count = 0;
            foreach (var file in Children)
                count += FileCount(file);
            return count;
        }

        int FileCount(IPackFile item)
        {
            if (item.PackFileType() == Common.PackFileType.Data)
                return 1;
        
            var count = 0;
            foreach (var child in item.Children)
                count += FileCount(child);
        
            return count;
        }

        PackFileDirectory CreateVirtualDirectory(string fullPath, string currentFolderPath, PackFileDirectory parent)
        {
            if (_directoryMap.ContainsKey(fullPath) == false)
            {
                var virtualDir = new PackFileDirectory(currentFolderPath);
                if (parent == null)
                    InternalFileList.Add(virtualDir.Name, virtualDir);
                else
                    parent.AddChild(virtualDir);
                _directoryMap.Add(fullPath, virtualDir);
                return virtualDir;
            }
            return _directoryMap[fullPath];
        }

        public void AddChild(IPackFile file)
        {
            file.Parent = this;
            InternalFileList.Add(file.Name, file);
        }

        public byte[] SaveToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {

                    
                }


                return ms.ToArray();
            }
        }

        public int ComputePackFileContentSize()
        {
            return 0;
        }

        public Dictionary<string, PackFile> CreateFileList()
        {
            var output = new Dictionary<string, PackFile>();
            foreach (var child in Children)
                CreateFileListRecursive(child, child.Name, output);
            return output;
        }

        void CreateFileListRecursive(IPackFile root, string currentPath, Dictionary<string, PackFile> output)
        {
            foreach (var child in root.Children)
            {
                if (child is PackFile)
                    output.Add(currentPath + "\\" + child.Name, child as PackFile);
                else
                    CreateFileListRecursive(child, currentPath + "\\" + child.Name, output);
            }
        }
    }
}
