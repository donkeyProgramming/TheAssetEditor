using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FileTypes.PackFiles.Models
{
    public class PackFileDataBase : NotifyPropertyChangedImpl
    {
        public List<PackFileContainer> PackFiles { get; set; } = new List<PackFileContainer>();

        public void AddPackFile(PackFileContainer pf)
        {
            PackFiles.Add(pf);
        }

        public void Clear()
        {
            PackFiles.Clear();
        }
    }
}
