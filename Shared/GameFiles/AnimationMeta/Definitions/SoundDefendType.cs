// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("SOUND_ATTACK_TYPE", 3)]
    public class SoundAttackType_v3 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt1_v3 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt2_v3 { get; set; }
    }

    [MetaData("SOUND_DEFEND_TYPE", 3)]
    public class SoundDefendType_v3 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt1_v3 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt2_v3 { get; set; }
    }


    [MetaData("SOUND_DEFEND_TYPE", 10)]
    public class SoundDefendType_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt1_v3 { get; set; }

        [MetaDataTag(7)]
        public int UnknownInt2_v3 { get; set; }
    }

    [MetaData("SOUND_DEFEND_TYPE", 11)]
    public class SoundDefendType_v11 : SoundDefendType_v10
    {
        [MetaDataTag(8)]
        public int UnknownInt0_v11 { get; set; }
    }


    [MetaData("SOUND_DEFEND_TYPE", 11, MetaDataAttributePriority.Low)]
    public class SoundDefendType_v11_Troy : SoundDefendType_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0_v11_troy { get; set; }
    }
}
