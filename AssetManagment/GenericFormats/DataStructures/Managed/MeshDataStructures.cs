using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using System.Collections.Generic;


namespace AssetManagement.GenericFormats.DataStructures.Managed
{
    public class PackedMesh
    {
        public string Name { get; set; }
        public List<ExtPackedCommonVertex> Vertices { get; set; }
        public List<ushort> Indices { get; set; }
        public List<ExtVertexWeight> VertexWeights { get; set; }
    }

    public class VertexWeight
    {     
        public string BoneName {get; set;}
        public int BoneIndex { get; set; }
        public int VertexIndex { get; set; }
        public float Weight { get; set; }
    }
};


