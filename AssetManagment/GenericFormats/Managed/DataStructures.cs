using AssetManagement.GenericFormats.Unmanaged;
using System.Collections.Generic;

namespace AssetManagement.GenericFormats.Managed
{
public class PackedMesh
{
    public string Name { get; set; }
    public List<ExtPackedCommonVertex> Vertices { get; set; }
    public List<ushort> Indices { get; set; }
    public List<VertexWeight> VertexWeights { get; set; }
}
};


