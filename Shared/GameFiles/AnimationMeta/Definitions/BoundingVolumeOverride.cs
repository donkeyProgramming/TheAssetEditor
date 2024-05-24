// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{

    [MetaData("BOUNDING_VOLUME_OVERRIDE", 10)]
    public class BoundingVolumeOverride_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Unk0 { get; set; }
    }
}
