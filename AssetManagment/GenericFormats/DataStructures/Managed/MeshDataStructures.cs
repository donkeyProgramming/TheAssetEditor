using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.Marshalling;
using Microsoft.Xna.Framework;
using System.Collections.Generic;


namespace AssetManagement.GenericFormats.DataStructures.Managed
{
    public class PackedMesh
    {
        public string Name { set; get; }
        public List<ExtPackedCommonVertex> Vertices { set; get; }
        public List<ushort> Indices { set; get; }
        public List<ExtVertexWeight> VertexWeights { set; get; }
    }

    public class PackCommonVertex : IMarshalable<ExtPackedCommonVertex>
    {
        public Vector4 Position { set; get; }
        public Vector3 Normal { set; get; }
        public Vector3 Bitangent { set; get; }
        public Vector3 Tangent { set; get; }
        public Vector2 Uv { set; get; }
        public Vector4 Color { set; get; }

        public override void FillFromStruct(in ExtPackedCommonVertex srcStruct)
        {
            Position = FromNativeHelpers.GetVector(srcStruct.Position);

            Normal = new Vector3(srcStruct.Normal.x, srcStruct.Normal.y, srcStruct.Normal.z);
            Bitangent = new Vector3(srcStruct.Bitangent.x, srcStruct.Bitangent.y, srcStruct.Bitangent.z);
            Tangent = new Vector3(srcStruct.Tangent.x, srcStruct.Tangent.y, srcStruct.Tangent.z);

            Uv = new Vector2(srcStruct.Uv.x, srcStruct.Uv.y);                    
        }

        public override void FillStruct(out ExtPackedCommonVertex destStruct)
        {
            destStruct.Position = ToNativeHelpers.GetFloat(Position);
            destStruct.Normal = ToNativeHelpers.GetFloat(Normal);
            destStruct.Tangent = ToNativeHelpers.GetFloat(Tangent);
            destStruct.Bitangent = ToNativeHelpers.GetFloat(Bitangent);
            destStruct.Uv = ToNativeHelpers.GetFloat(Uv);
            destStruct.Color = ToNativeHelpers.GetFloat(Color);
        }
    }



    //public class PackedMesh
    //{
    //    public string Name { get; set; }
    //    public List<ExtPackedCommonVertex> Vertices { get; set; }
    //    public List<ushort> Indices { get; set; }
    //    public List<ExtVertexWeight> VertexWeights { get; set; }
    //}

    public class VertexWeight
    {     
        public string BoneName {get; set;}
        public int BoneIndex { get; set; }
        public int VertexIndex { get; set; }
        public float Weight { get; set; }
    }
};


