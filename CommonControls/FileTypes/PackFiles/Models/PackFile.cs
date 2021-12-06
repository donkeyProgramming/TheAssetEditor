using Common;
using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonControls.FileTypes.PackFiles.Models
{
    public class PackFile : NotifyPropertyChangedImpl
    {
        public IDataSource DataSource { get; set; }

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

}
