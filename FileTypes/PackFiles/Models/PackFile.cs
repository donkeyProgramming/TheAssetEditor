using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FileTypes.PackFiles.Models
{
    public class PackFile : NotifyPropertyChangedImpl, IPackFile
    {
        public PackFileType PackFileType() { return Common.PackFileType.Data; }
        public IDataSource DataSource { get; private set; }

        public PackFile(string packContainerPath, string name, string fullPath, long dataOffset = 0, long dataLength = 0)
        {
            Name = name;
            FullPath = fullPath;
            DataSource = new PackedFileSource(packContainerPath, dataOffset, dataLength);
        }

        string _name;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }
        public string FullPath { get; set; }


        bool _isEdited;
        public bool IsEdited
        {
            get => _isEdited;
            set => SetAndNotify(ref _isEdited, value);
        }

        public override string ToString() { return Name; }
        public void Sort(){}

        IEnumerable<IPackFile> IPackFile.FileChildren => Enumerable.Empty<IPackFile>();
        IEnumerable<IPackFile> IPackFile.FolderChildren => Enumerable.Empty<IPackFile>();
    }


    public class PackFileDirectory : NotifyPropertyChangedImpl, IPackFile
    {
        public PackFileDirectory(string name) 
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"Dir : {Name} - Files: {InternalFileList.Count}";
        }

        string _name;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        public void AddChild(IPackFile file)
        {
            if (file.PackFileType() == Common.PackFileType.Data)
                InternalFileList.Add(file.Name, file);
            else
                InternalFolderList.Add(file.Name, file);
        }

        public void Sort()
        {
            InternalFileList = new Dictionary<string, IPackFile>(InternalFileList.OrderBy(x => x.Value.PackFileType()).ThenBy(x => x.Value.Name));
            foreach (var item in InternalFileList)
                item.Value.Sort();
        }

        public PackFileType PackFileType() { return Common.PackFileType.Directory; }

    
        Dictionary<string, IPackFile> InternalFileList { get; set; } = new Dictionary<string, IPackFile>();
        Dictionary<string, IPackFile> InternalFolderList { get; set; } = new Dictionary<string, IPackFile>();

        IEnumerable<IPackFile> IPackFile.FileChildren => InternalFileList.Values;
        IEnumerable<IPackFile> IPackFile.FolderChildren => InternalFolderList.Values;
    }
}
