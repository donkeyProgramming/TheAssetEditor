using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.MetaData
{
    public class MetaDataTagItem
    {
        public class Data
        {
            public Data(string parentFileName, byte[] bytes, int start, int size)
            {
                ParentFileName = parentFileName;
                Bytes = bytes;
                Start = start;
                Size = size;
            }
            public string ParentFileName { get; set; }

            public byte[] Bytes { get; set; }
            public int Start { get; set; }
            public int Size { get; set; }

            public int Version
            {
                get { return BitConverter.ToInt32(Bytes, Start); }
            }
        }

        public string Name { get; set; } = "";
        public int Version { get; set; }
        public List<Data> DataItems { get; set; } = new List<Data>();

        public string DisplayNameWithCount { get { return $"{Name}_v{Version} [{DataItems.Count}]"; } }
        public string DisplayName { get { return $"{Name}_v{Version}"; } }

        public bool IsDecodedCorrectly { get; set; } = false;

        public override string ToString()
        {
            return $"{Name} - {Version}, count = {DataItems.Count}";
        }
    }
}
