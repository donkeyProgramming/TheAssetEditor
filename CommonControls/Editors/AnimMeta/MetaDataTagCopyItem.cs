using CommonControls.Common;
using CommonControls.FileTypes.MetaData;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Editors.AnimMeta
{
    public class MetaDataTagCopyItem : ICopyPastItem
    {
        public string Description { get; set; } = "Copy object for MetaDataTag";
        public UnknownMetaEntry Data { get; set; }
    }
}
