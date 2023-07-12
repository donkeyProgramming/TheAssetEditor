// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AnimationMeta.FileTypes.Parsing;
using Microsoft.Xna.Framework;

namespace AnimationMeta.FileTypes.Definitions
{
    [MetaData("TARGET_POS", 0)]
    public class TargetPos_0 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2)]
        public Vector3 Position { get; set; } = Vector3.Zero;
    }

    [MetaData("TARGET_POS", 10)]
    public class TargetPos_10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public Vector3 Position { get; set; } = Vector3.Zero;
    }
}
