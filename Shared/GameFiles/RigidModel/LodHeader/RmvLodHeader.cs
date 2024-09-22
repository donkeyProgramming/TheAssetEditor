namespace Shared.GameFormats.RigidModel.LodHeader
{
    /*
 * FROM CA: https://discord.com/channels/373745291289034763/448884160094797834/766300715294523423
    struct rigid_lod_header 
    {
      unit32_t mesh_count;             // Number of meshes in LOD
      unit32_t total_lod_vertex_size;  // Number of vertices for all meshes in LOD
      unit32_t total_lod_index_size;   // Number of indices for all meshes in LOD
      unit32_t first_mesh_offset;      // Offset in bytes from start of file to the first instance of mesh header in LOD
      float32_t lod_distance;          // Distance until which this LOD should be displayed
      unit32_t authored_lod_index;     // Numerical indexz of this LOD as made by the artist
      unit8_t quality_level;           // The lowest graphics quality level that this mesh LOD will be active. Zero is the lowest graphics quality, meaning LODs flagged with quality level zero will be visible on graphics settings. 
    } 
 */

    public interface RmvLodHeader
    {
        uint MeshCount { get; set; }
        uint TotalLodVertexSize { get; set; }
        uint TotalLodIndexSize { get; set; }
        uint FirstMeshOffset { get; set; }
        byte QualityLvl { get; set; }
        float LodCameraDistance { get; set; }

        int GetHeaderSize();


        public RmvLodHeader Clone();
    }

    public interface ILodHeaderCreator
    {
        uint HeaderSize { get; }
        RmvLodHeader Create(byte[] buffer, int offset);
        RmvLodHeader CreateFromBase(RmvLodHeader source, uint lodLevel);
        RmvLodHeader CreateEmpty(float cameraDistance, uint lodLevel, byte qualityLevel);
        byte[] Save(RmvLodHeader rmvLodHeader);

    }
}
