using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public class TerrainTileMaterial : IRmvMaterial
    {
        public VertexFormat BinaryVertexFormat { get; set; }
        public Vector3 PivotPoint { get; set; } = Vector3.Zero;
        public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;
        public string ModelName { get; set; } = "TerrainTile";

        public string TerrainBaseStr { get; set; }
        public ModelMaterialEnum MaterialId { get; set; } = ModelMaterialEnum.TerrainTiles;
        public string TextureDirectory { get => ""; set { } }

        uint Unknown0 { get; set; }
        uint Unknown1 { get; set; }
        uint Unknown2 { get; set; }
        uint Unknown3 { get; set; }
        uint Unknown4 { get; set; }
        uint Unknown5 { get; set; }


        public IRmvMaterial Clone()
        {
            return new TerrainTileMaterial()
            {
                MaterialId = MaterialId,
                TerrainBaseStr = TerrainBaseStr,
                Unknown0 = Unknown0,
                Unknown1 = Unknown1,
                Unknown2 = Unknown2,
                Unknown3 = Unknown3,
                Unknown4 = Unknown4,
                Unknown5 = Unknown5,
                AlphaMode = AlphaMode,
                BinaryVertexFormat = BinaryVertexFormat,
                ModelName = ModelName,
                PivotPoint = PivotPoint,

            };
        }

        public uint ComputeSize()
        {
            return (uint)ByteHelper.GetSize<TerrainTileStruct>();
        }

        public RmvTexture? GetTexture(TextureType texureType)
        {
            return null;
        }

        public List<RmvTexture> GetAllTextures()
        {
            return new List<RmvTexture>();
        }
        public void SetTexture(TextureType texureType, string path)
        {
        }

        public void UpdateInternalState(UiVertexFormat uiVertexFormat)
        {
            throw new NotImplementedException();
        }

        public void EnrichDataBeforeSaving(List<RmvAttachmentPoint> attachmentPoints, int matrixOverride)
        {
            throw new NotImplementedException();
        }
    }

    public class TerrainTileMaterialCreator : IMaterialCreator
    {
        public IRmvMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<TerrainTileStruct>(buffer, offset);
            return new TerrainTileMaterial()
            {
                MaterialId = materialId,
                BinaryVertexFormat = VertexFormat.Position16_bit,
            };
        }

        public IRmvMaterial CreateEmpty(ModelMaterialEnum materialId)
        {
            return new TerrainTileMaterial();
        }

        public byte[] Save(IRmvMaterial material)
        {
            throw new NotImplementedException();
        }
    }

#pragma warning disable CS0649 
    struct TerrainTileStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Name;

        public uint Unknown0;
        public uint Unknown1;
        public uint Unknown2;
        public uint Unknown3;
        public uint Unknown4;
        public uint Unknown5;
    }
#pragma warning restore CS0649
}
