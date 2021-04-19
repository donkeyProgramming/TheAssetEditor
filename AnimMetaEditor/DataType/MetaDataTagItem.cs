using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimMetaEditor.DataType
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

        public string DisplayName { get { return $"{Name}_v{Version} [{DataItems.Count}]"; } }

        public override string ToString()
        {
            return $"{Name} - {Version}, count = {DataItems.Count}";
        }
    }


}
