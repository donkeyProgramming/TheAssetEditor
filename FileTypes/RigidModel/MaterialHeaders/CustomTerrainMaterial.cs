using Filetypes;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FileTypes.RigidModel.MaterialHeaders
{

    public class CustomTerrainMaterial : IMaterial
    {
        public VertexFormat BinaryVertexFormat { get; set; } = VertexFormat.CustomTerrain;
        public ModelMaterialEnum MaterialId { get; set; } = ModelMaterialEnum.TerrainTiles;
        UiVertexFormat IMaterial.VertexType { get; set; } = UiVertexFormat.Static;

        public Vector3 PivotPoint { get; set; } = Vector3.Zero;
        public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;
        public string ModelName { get; set; } = "TerrainTile";

        public string TexturePath { get; set; }


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

        public RmvTexture? GetTexture(TexureType texureType)
        {
            return null;
        }

        public void UpdateBeforeSave(UiVertexFormat uiVertexFormat, RmvVersionEnum outputVersion, string[] boneNames)
        {
            throw new NotImplementedException();
        }

        public void SetTexture(TexureType texureType, string path)
        {
        }
    }

    public class CustomTerrainMaterialCreator : IMaterialCreator
    {
        public IMaterial Create(RmvVersionEnum rmvType, byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<CustomTerrainStruct>(buffer, offset);
            return new CustomTerrainMaterial()
            {
                TexturePath = Util.SanatizeFixedString(Encoding.ASCII.GetString(header.TexturePath)),
            };
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
