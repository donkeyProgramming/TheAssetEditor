using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public enum PackFileType
    {
        PackContainer = 2,
        Data = 1,
        Directory = 0
    }


    public interface IPackFile
    {
        string Name { get; set; }
    }

}
