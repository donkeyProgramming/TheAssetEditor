// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("IMPACT_POS", 2)]
    public class ImpactPosition_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public Vector3 Position { get; set; }
    }

    [MetaData("IMPACT_POS", 10)]
    public class ImpactPosition_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public Vector3 Position { get; set; }
    }

    [MetaData("IMPACT_DIRECTION_POS", 10)]
    public class ImpactDirectionPosition_v10 : ImpactPosition_v10
    {
    }
}
