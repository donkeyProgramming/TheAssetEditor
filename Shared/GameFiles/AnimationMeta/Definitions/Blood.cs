// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("BLOOD", 5)]
    public class Blood_v5 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(5)]
        public string UnknownBoolAsStr { get; set; } = "";

        [MetaDataTag(6)]
        public Vector3 Position { get; set; }

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; }

        [MetaDataTag(8, "Bone the effect is attached to")]
        public int NodeIndex { get; set; }

        [MetaDataTag(9, "Scale of the effect")]
        public float Scale { get; set; }
    }


    [MetaData("BLOOD", 11)]
    public class Blood_v11 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Name of the VFX's .xml file in the vfx folder. Leave off the file extension. Note that for this you don't need to add custom vfx to the particles db table and they still require a \"movie\"-type .pack for them to be loaded.")]
        public string VfxName { get; set; } = "";

        [MetaDataTag(6)]
        public int Unk { get; set; }

        [MetaDataTag(7)]
        public bool Tracking { get; set; }

        [MetaDataTag(8)]
        public bool TerrainMutable { get; set; }

        [MetaDataTag(9)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(10)]
        public Vector3 Position { get; set; }

        [MetaDataTag(11, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; }

        [MetaDataTag(12, "Bone the effect is attached to")]
        public int NodeIndex { get; set; }

        [MetaDataTag(13, "Scale of the effect")]
        public float Scale { get; set; }
    }

    [MetaData("BLOOD", 12)]
    public class Blood_v12 : Blood_v11
    {
        // new field for v12
        [MetaDataTag(10)]
        public bool UnknownBool_v12 { get; set; }


        //override MetaDataTag order
        [MetaDataTag(11)]
        public new Vector3 Position { get; set; }

        [MetaDataTag(12, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public new Vector4 Orientation { get; set; }

        [MetaDataTag(13, "Bone the effect is attached to")]
        public new int NodeIndex { get; set; }

        [MetaDataTag(14, "Scale of the effect")]
        public new float Scale { get; set; }
    }
}
