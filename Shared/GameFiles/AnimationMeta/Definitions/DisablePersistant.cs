using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("DISABLE_PERSISTENT", 2)]
    public class DisablePersistant_v2 : DecodedMetaEntryBase_v2
    {
    }

    [MetaData("DISABLE_PERSISTENT", 10)]
    public class DisablePersistant_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("DISABLE_PERSISTENT_VFX", 10)]
    public class DisablePersistantVfx_v10 : DecodedMetaEntryBase
    {
    }
}
