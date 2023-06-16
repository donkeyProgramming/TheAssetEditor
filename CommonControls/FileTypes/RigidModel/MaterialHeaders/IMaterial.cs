using CommonControls.FileTypes.RigidModel.Types;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace CommonControls.FileTypes.RigidModel.MaterialHeaders
{
    public interface IMaterial
    {
        public ModelMaterialEnum MaterialId { get; set; }
        VertexFormat BinaryVertexFormat { get; set; }
        Vector3 PivotPoint { get; set; }
        AlphaMode AlphaMode { get; set; }
        string ModelName { get; set; }
        string TextureDirectory { get; set; }

        IMaterial Clone();
        uint ComputeSize();
        List<RmvTexture> GetAllTextures();
        RmvTexture? GetTexture(TextureType texureType);
        void SetTexture(TextureType texureType, string path);

        void UpdateEnumsBeforeSaving(UiVertexFormat uiVertexFormat, RmvVersionEnum outputVersion);
        void EnrichDataBeforeSaving(string[] boneNames, BoundingBox boundingBox);
    }

    public interface IMaterialCreator
    {
        IMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] dataArray, int dataOffset);
        IMaterial CreateEmpty(ModelMaterialEnum materialId, RmvVersionEnum rmvType, VertexFormat vertexFormat);
        byte[] Save(IMaterial material);
    }
}
