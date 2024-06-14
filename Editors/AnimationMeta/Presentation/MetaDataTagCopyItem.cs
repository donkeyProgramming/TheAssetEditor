using Shared.Core.Misc;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.Presentation
{
    public class MetaDataTagCopyItem : ICopyPastItem
    {
        public string Description { get; set; } = "Copy object for MetaDataTag";
        public UnknownMetaEntry Data { get; set; }
    }
}
