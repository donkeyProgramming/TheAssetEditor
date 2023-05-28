using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("VOLUMETRIC_EFFECT", 2)]
    public class VolumetricEffect_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)] 
        public string VfxString { get; set; } = "";
        
        [MetaDataTag(5)]
        public Vector3 Unknown0_v2 { get; set; }
        
        [MetaDataTag(6)]
        public Vector4 Unknown1_v2 { get; set; }
        
        [MetaDataTag(7)]
        public Vector3 Unknown2_v2 { get; set; }
        
        [MetaDataTag(8)]
        public Vector3 Unknown3_v2 { get; set; }
        
        [MetaDataTag(9)]
        public Vector4 Unknown4_v2 { get; set; }
        
        [MetaDataTag(10)]
        public Vector3 Unknown5_v2 { get; set; }
    }
    
    [MetaData("VOLUMETRIC_EFFECT", 10)]
    public class VolumetricEffect_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)] 
        public string VfxString { get; set; } = "";
        
        [MetaDataTag(6)]
        public Vector3 Unknown0_v10 { get; set; }
        
        [MetaDataTag(7)]
        public Vector4 Unknown1_v10 { get; set; }
        
        [MetaDataTag(8)]
        public Vector3 Unknown2_v10 { get; set; }
        
        [MetaDataTag(9)]
        public Vector3 Unknown3_v10 { get; set; }
        
        [MetaDataTag(10)]
        public Vector4 Unknown4_v10 { get; set; }
        
        [MetaDataTag(11)]
        public Vector3 Unknown5_v10 { get; set; }
    }
}
