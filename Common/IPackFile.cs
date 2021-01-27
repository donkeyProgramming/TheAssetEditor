using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public enum PackFileType
    {
        Data = 1,
        Directory = 0
    }


    public interface IPackFile
    {
        IEnumerable<IPackFile> Children { get; }
        string Name { get; set; }
        PackFileType PackFileType();
        void Sort();
    }

}
