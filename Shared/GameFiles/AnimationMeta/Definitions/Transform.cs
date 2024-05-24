// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("TRANSFORM", 10)]
    public class Transform_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "ID of the bone which you are moving or rotating.")]
        public int TargetNode { get; set; }

        [MetaDataTag(6)]
        public int SourceNode { get; set; }
        [MetaDataTag(7)]
        public Vector3 Position { get; set; } = Vector3.Zero;
        [MetaDataTag(8, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);
        [MetaDataTag(9)]
        public float BlendInTime { get; set; }
        [MetaDataTag(10)]
        public float BlendOutTime { get; set; }
    }
}
