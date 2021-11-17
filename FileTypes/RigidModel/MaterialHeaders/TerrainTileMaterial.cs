using Filetypes;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FileTypes.RigidModel.MaterialHeaders
{
    public class TerrainTileMaterial : IMaterial
    {
        public VertexFormat VertexType { get; set; }
        public VertexFormat BinaryVertexFormat { get; set; }
        public Vector3 PivotPoint { get; set; } = Vector3.Zero;
        public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;
        public string ModelName { get; set; } = "TerrainTile";

        public string TerrainBaseStr { get; set; }

        uint Unknown0 { get; set; }
        uint Unknown1 { get; set; }
        uint Unknown2 { get; set; }
        uint Unknown3 { get; set; }
        uint Unknown4 { get; set; }
        uint Unknown5 { get; set; }

        public IMaterial Clone()
        {
            return new TerrainTileMaterial()
            { 
                AlphaMode = AlphaMode,
                BinaryVertexFormat = BinaryVertexFormat,
                ModelName = ModelName,
                PivotPoint = PivotPoint,
                VertexType = VertexType,
            };
        }

        public uint ComputeSize()
        {
            throw new NotImplementedException();
        }

        public RmvTexture? GetTexture(TexureType texureType)
        {
            return null;
        }
    }


    public class TerrainTileMaterialCreator : IMaterialCreator
    {
        public IMaterial Create(RmvVersionEnum rmvType, byte[] buffer, int offset)
        {
            var header = ByteHelper.ByteArrayToStructure<TerrainTileStruct>(buffer, offset);
            return new TerrainTileMaterial()
            {
                VertexType = VertexFormat.Position16_bit,
                BinaryVertexFormat = VertexFormat.Position16_bit,
            };
        }

        public byte[] Save(IMaterial material)
        {
            throw new NotImplementedException();
        }
    }

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
}
