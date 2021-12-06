using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonControls.FileTypes.MetaData
{
    public class MetaDataFile
    {
        public int Version { get; set; }
        public List<IMetaEntry> Items { get; set; } = new List<IMetaEntry>();

        public List<MetaEntry> GetItemsOfType(string type)
        {
            return Items
                .Where(x => x.DecodedCorrectly && x is MetaEntry)
                .Where(x => x.Name.Contains(type, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x as MetaEntry)
                .ToList(); ;
        }
    }
}
