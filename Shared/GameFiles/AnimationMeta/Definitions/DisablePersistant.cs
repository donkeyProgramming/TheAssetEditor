// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
