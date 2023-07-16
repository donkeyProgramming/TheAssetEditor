using AssetManagement.GenericFormats.Unmanaged;
using System.Collections.Generic;

namespace AssetManagement.GenericFormats.Managed
{
    public class PackedMesh
    {
        public string Name { set; get; }
        public List<PackedCommonVertex> Vertices { set; get; }
        public List<ushort> Indices { set; get; }
        public List<VertexWeight> VertexWeights { set; get; }
    }
};


