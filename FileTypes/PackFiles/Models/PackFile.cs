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
        public PackFile(string packContainerPath, string name, string fullPath, long dataOffset = 0, long dataLength = 0)
        {
            Name = name;
            FullPath = fullPath;
            DataSource = new PackedFileSource(packContainerPath, dataOffset, dataLength);
        }

        public IDataSource DataSource { get; private set; }

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


        public PackFileType PackFileType() { return Common.PackFileType.Data; }

        
        public override string ToString()
        {
            return $"Data : {Name}";
        }

        public void Sort()
        {

        }

        IEnumerable<IPackFile> IPackFile.Children => Enumerable.Empty<IPackFile>();
    }


    public class PackFileDirectory : NotifyPropertyChangedImpl, IPackFile
    {
        public PackFileDirectory(string name) 
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"Dir : {Name} - Files: {InternalList.Count}";
        }

        string _name;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        public void AddChild(IPackFile file)
        {
            InternalList.Add(file.Name, file);
        }

        public void Sort()
        {
            InternalList = new Dictionary<string, IPackFile>(InternalList.OrderBy(x => x.Value.PackFileType()).ThenBy(x => x.Value.Name));
            foreach (var item in InternalList)
                item.Value.Sort();
        }

        public PackFileType PackFileType() { return Common.PackFileType.Directory; }

        Dictionary<string, IPackFile> InternalList { get; set; } = new Dictionary<string, IPackFile>();

        IEnumerable<IPackFile> IPackFile.Children => InternalList.Values;
    }
}
