using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SNIP", 10)]
    public class Snip : MetaEntryBase
    {
        [MetaDataTag(5, "")]
        public float Scale { get; set; }
    }
}
