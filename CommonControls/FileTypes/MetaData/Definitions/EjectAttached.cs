using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    [MetaData("EJECT_ATTACHED", 10)]
    public class EjectAttached_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public Vector3 Direction { get; set; }
    }
}
