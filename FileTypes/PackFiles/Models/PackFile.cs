using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypes.PackFiles.Models
{
    public class PackFile : NotifyPropertyChangedImpl, IPackFile
    {
        public IDataSource DataSource { get; private set; }

        public PackFile(string name, IDataSource dataSource)
        {
            Name = name;
            DataSource = dataSource;
        }

        string _name;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        bool _isEdited;
        public bool IsEdited
        {
            get => _isEdited;
            set => SetAndNotify(ref _isEdited, value);
        }

        public override string ToString() { return Name; }

        public string Extention { get => Path.GetExtension(Name); }
    }





    /*public class PackFileDirectory : NotifyPropertyChangedImpl, IPackFile
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


        public void Sort()
        {
            InternalFileList = new Dictionary<string, IPackFile>(InternalFileList.OrderBy(x => x.Value.PackFileType()).ThenBy(x => x.Value.Name));
            foreach (var item in InternalFileList)
                item.Value.Sort();
        }

        public PackFileType PackFileType() { return Common.PackFileType.Directory; }
        public IPackFile FindChild(string itemName)
        {
            InternalFileList.TryGetValue(itemName, out var value);
            return value;
        }

    
        Dictionary<string, IPackFile> InternalFileList { get; set; } = new Dictionary<string, IPackFile>();

        IEnumerable<IPackFile> IPackFile.Children => InternalFileList.Values;

        IPackFile IPackFile.Parent { get; set; }
    }*/
}
