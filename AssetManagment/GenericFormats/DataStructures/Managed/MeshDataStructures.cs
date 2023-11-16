using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using System;
using System.Collections.Generic;


namespace AssetManagement.GenericFormats.DataStructures.Managed
{
    public class PackedMesh
    {
        public string Name { get; set; }
        public List<ExtPackedCommonVertex> Vertices { get; set; }
        public List<uint> Indices { get; set; }
        public List<ExtVertexWeight> VertexWeights { get; set; }
    }
};


