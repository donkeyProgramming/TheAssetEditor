using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimMetaEditor.DataType
{
    public class MetaDataFile
    {
        public int Version { get; set; }
        public string FileName { get; set; }
        public List<MetaDataTagItem> TagItems { get; set; } = new List<MetaDataTagItem>();

        public override string ToString()
        {
            return $"{FileName} - {TagItems.Count}";
        }
    }
}
