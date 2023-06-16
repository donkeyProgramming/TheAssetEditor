using CommonControls.FileTypes;
using CommonControls.FileTypes.RigidModel.Types;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CommonControls.FileTypes.RigidModel.MaterialHeaders
{
    public class CustomTerrainMaterial : IMaterial
    {
        public VertexFormat BinaryVertexFormat { get; set; } = VertexFormat.CustomTerrain;
        public ModelMaterialEnum MaterialId { get; set; } = ModelMaterialEnum.TerrainTiles;

        public Vector3 PivotPoint { get; set; } = Vector3.Zero;
        public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;
        public string ModelName { get; set; } = "TerrainTile";

        public string TexturePath { get; set; }
        public string TextureDirectory { get => ""; set { } }

        public IMaterial Clone()
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


        public void UpdateEnumsBeforeSaving(UiVertexFormat uiVertexFormat, RmvVersionEnum outputVersion)
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

        public void EnrichDataBeforeSaving(string[] boneNames, BoundingBox boundingBox)
        {
            throw new NotImplementedException();
        }
    }

    public class CustomTerrainMaterialCreator : IMaterialCreator
    {
        public IMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<CustomTerrainStruct>(buffer, offset);
            return new CustomTerrainMaterial()
            {
                MaterialId = materialId,
                TexturePath = Util.SanatizeFixedString(Encoding.ASCII.GetString(header.TexturePath)),
            };
        }

        public IMaterial CreateEmpty(ModelMaterialEnum materialId, RmvVersionEnum rmvType, VertexFormat vertexFormat)
        { 
            return new CustomTerrainMaterial();
        }

        public byte[] Save(IMaterial material)
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
