// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{


    [MetaData("DISABLE_PERSISTENT_ID", 10)]
    public class DisablePersistantId_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int Value { get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7)]
        public float BlendOutTime { get; set; }
    }

    [MetaData("DISABLE_PERSISTENT_ID", 11)]
    public class DisablePersistantId_v11 : DisablePersistantId_v10
    {
        [MetaDataTag(8)]
        public int UnknownInt_v11 { get; set; }
    }

    [MetaData("DISABLE_PERSISTENT_ID", 11, MetaDataAttributePriority.Low)]
    public class DisablePersistantId_v11_Troy : DisablePersistantId_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte_v11_troy { get; set; }
    }

    [MetaData("DISABLE_PERSISTENT_ID", 12)]
    public class DisablePersistantId_v12 : DisablePersistantId_v11
    {
        // VECTOR3 + string/2xbool?
        [MetaDataTag(9)]
        public byte UnknownByte0_v12 { get; set; }
        [MetaDataTag(10)]
        public byte UnknownByte1_v12 { get; set; }
        [MetaDataTag(11)]
        public byte UnknownByte2_v12 { get; set; }
        [MetaDataTag(12)]
        public byte UnknownByte3_v12 { get; set; }
        [MetaDataTag(13)]
        public byte UnknownByte4_v12 { get; set; }
        [MetaDataTag(14)]
        public byte UnknownByte5_v12 { get; set; }
        [MetaDataTag(15)]
        public byte UnknownByte6_v12 { get; set; }
        [MetaDataTag(16)]
        public byte UnknownByte7_v12 { get; set; }
        [MetaDataTag(17)]
        public byte UnknownByte8_v12 { get; set; }
        [MetaDataTag(18)]
        public byte UnknownByte9_v12 { get; set; }
        [MetaDataTag(19)]
        public byte UnknownByte10_v12 { get; set; }
        [MetaDataTag(20)]
        public byte UnknownByte11_v12 { get; set; }
        [MetaDataTag(21)]
        public byte UnknownByte12_v12 { get; set; }
        [MetaDataTag(22)]
        public byte UnknownByte13_v12 { get; set; }
    }

    [MetaData("DISABLE_PERSISTENT_ID", 14)]
    public class DisablePersistantId_v14 : DisablePersistantId_v12
    {
        // int + bool ?
        [MetaDataTag(23)]
        public byte UnknownByte0_v14 { get; set; }
        [MetaDataTag(24)]
        public byte UnknownByte1_v14 { get; set; }
        [MetaDataTag(25)]
        public byte UnknownByte2_v14 { get; set; }
        [MetaDataTag(26)]
        public byte UnknownByte3_v14 { get; set; }
        [MetaDataTag(27)]
        public byte UnknownByte4_v14 { get; set; }
    }
}
