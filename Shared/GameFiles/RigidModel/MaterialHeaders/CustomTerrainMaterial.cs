using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{

    public class CustomTerrainMaterial : IRmvMaterial
    {
        public VertexFormat BinaryVertexFormat { get; set; } = VertexFormat.CustomTerrain;
        public ModelMaterialEnum MaterialId { get; set; } = ModelMaterialEnum.TerrainTiles;

        public Vector3 PivotPoint { get; set; } = Vector3.Zero;
        public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;
        public string ModelName { get; set; } = "TerrainTile";

        public string TexturePath { get; set; }
        public string TextureDirectory { get => ""; set { } }

        public IRmvMaterial Clone()
        {
            return new CustomTerrainMaterial()
            {
                MaterialId = MaterialId,
                TexturePath = TexturePath,
                AlphaMode = AlphaMode,
                BinaryVertexFormat = BinaryVertexFormat,
                ModelName = ModelName,
                PivotPoint = PivotPoint,
            };
        }

        public uint ComputeSize()
        {
            return (uint)ByteHelper.GetSize<CustomTerrainStruct>();
        }

        public RmvTexture? GetTexture(TextureType texureType)
        {
            return null;
        }


        public void UpdateInternalState(UiVertexFormat uiVertexFormat)
        {
            throw new NotImplementedException();
        }

        public void SetTexture(TextureType texureType, string path)
        {
        }

        public List<RmvTexture> GetAllTextures()
        {
            return new List<RmvTexture>();
        }

        public void EnrichDataBeforeSaving(List<RmvAttachmentPoint> attachmentPoints, int matrixOverride)
        {
            throw new NotImplementedException();
        }
    }

    public class CustomTerrainMaterialCreator : IMaterialCreator
    {
        public IRmvMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<CustomTerrainStruct>(buffer, offset);
            return new CustomTerrainMaterial()
            {
                MaterialId = materialId,
                TexturePath = StringSanitizer.FixedString(Encoding.ASCII.GetString(header.TexturePath)),
            };
        }

        public IRmvMaterial CreateEmpty(ModelMaterialEnum materialId)
        {
            return new CustomTerrainMaterial();
        }

        public byte[] Save(IRmvMaterial material)
        {
            throw new NotImplementedException();
        }
    }

    struct CustomTerrainStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] TexturePath;
    }
}
