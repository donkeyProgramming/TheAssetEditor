using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public interface IRmvMaterial
    {
        public ModelMaterialEnum MaterialId { get; }
        VertexFormat BinaryVertexFormat { get; }
        Vector3 PivotPoint { get; set; }
        AlphaMode AlphaMode { get; set; }
        string ModelName { get; set; }
        string TextureDirectory { get; set; }

        IRmvMaterial Clone();
        uint ComputeSize();
        List<RmvTexture> GetAllTextures();
        RmvTexture? GetTexture(TextureType texureType);
        void SetTexture(TextureType texureType, string path);

        void UpdateEnumsBeforeSaving(UiVertexFormat uiVertexFormat, RmvVersionEnum outputVersion);
        void EnrichDataBeforeSaving(string[] boneNames);
    }

    public interface IMaterialCreator
    {
        IRmvMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] dataArray, int dataOffset);
        IRmvMaterial CreateEmpty(ModelMaterialEnum materialId, VertexFormat vertexFormat);
        byte[] Save(IRmvMaterial material);
    }
}
