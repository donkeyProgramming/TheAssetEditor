using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public interface IRmvMaterial
    {
        public ModelMaterialEnum MaterialId { get; }
        VertexFormat BinaryVertexFormat { get; }
        Vector3 PivotPoint { get; set; }
        string ModelName { get; set; }
        string TextureDirectory { get; set; }

        IRmvMaterial Clone();
        uint ComputeSize();
        List<RmvTexture> GetAllTextures();
        RmvTexture? GetTexture(TextureType texureType);
        void SetTexture(TextureType texureType, string path);

        void UpdateInternalState(UiVertexFormat uiVertexFormat);
        void EnrichDataBeforeSaving(List<RmvAttachmentPoint> attachmentPoints, int matrixOverride);
    }

    public interface IMaterialCreator
    {
        IRmvMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] dataArray, int dataOffset);
        IRmvMaterial CreateEmpty(ModelMaterialEnum materialId);
        byte[] Save(IRmvMaterial material);
    }
}
