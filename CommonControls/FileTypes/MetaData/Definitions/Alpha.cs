using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("ALPHA", 10)]
    public class Alpha_v10 : MetaEntryBase
    {
        [MetaDataTag(5, "This might be a %, 0 = invisible and 1 = visible.")]
        public float DesiredAlpha { get; set; }
    }
}
