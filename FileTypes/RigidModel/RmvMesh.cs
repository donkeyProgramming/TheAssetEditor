using Filetypes.ByteParsing;
using Filetypes.RigidModel.Transforms;
using Filetypes.RigidModel.Vertex;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RmvModelHeader
    { 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] _fileType;

        public uint Version;
        public uint LodCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        byte[] _skeletonName;

        public string FileType
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_fileType, 0, 4, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
            }
        }

        public string SkeletonName
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_skeletonName, 0, 128, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
            }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RmvSubModelHeader
    {
        public GroupTypeEnum MaterialId;
        public uint ModelSize;
        public uint VertexOffset;
        public uint VertexCount;
        public uint FaceOffset;
        public uint FaceCount;

        public RvmBoundingBox BoundingBox;
        public RmvShaderParams ShaderParams;

        ushort _vertexType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public  byte[] _modelName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] _textureDir;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] ZeroPadding0;

        public byte _unknown0;
        public byte _unknown1;

        public RmvTransform Transform;

        public int LinkDirectlyToBoneIndex;
        public int Flag_alwaysNegativeOne;
        public uint AttachmentPointCount;
        public uint TextureCount;

        public UknownData _uknownData;

        public VertexFormat VertextType { get => (VertexFormat)_vertexType; }

        public string ModelName
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_modelName, 0, 32, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
            }
            set 
            {
                for (int i = 0; i < 32; i++)
                    _modelName[i] = 0;

                var byteValues = Encoding.UTF8.GetBytes(value);
                for (int i = 0; i < byteValues.Length; i++)
                {
                    _modelName[i] = byteValues[i];
                }
            }
        }

        public string TextureDirectory
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_textureDir, 0, 256, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        internal void UpdateHeader(RmvMesh mesh, List<RmvTexture> textures, List<RmvAttachmentPoint> attachmentPoints)
        {
            VertexCount = (uint)mesh.VertexList.Length;
            FaceCount = (uint)mesh.IndexList.Length;
            AttachmentPointCount = (uint)attachmentPoints.Count;
            TextureCount = (uint)textures.Count;

            var nonMeshSize = (uint)(ByteHelper.GetSize<RmvSubModelHeader>() +
                (ByteHelper.GetSize<RmvAttachmentPoint>() * AttachmentPointCount) +
                (ByteHelper.GetSize<RmvTexture>() * TextureCount) +
                ByteHelper.GetSize<MeshAlphaSettings>());

            ModelSize = +nonMeshSize +
                (uint)
                ((RmvMesh.GetVertexSize(VertextType) * VertexCount) +
                (sizeof(ushort) * FaceCount));

            VertexOffset = nonMeshSize;
            FaceOffset = nonMeshSize + (uint)(RmvMesh.GetVertexSize(VertextType) * VertexCount);

            BoundingBox.Recompute(mesh);
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RmvShaderParams
    {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        byte[] _shaderName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] UnknownValues;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] AllZeroValues;

        public string ShaderName
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_shaderName, 0, 12, out string value, out _);
                if (result == false)
                    throw new Exception();
                return value;
            }
        }
    }

    [Serializable]
    public struct UknownData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        byte[] zeroPadding0;

        byte alwaysOne;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 131)]
        byte[] zeroPadding1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshAlphaSettings
    {
        public int KeyValue;
        public AlphaMode Mode;
    }

    public class RmvMesh
    {
        public BaseVertex[] VertexList { get; set; }
        public ushort[] IndexList;
        public MeshAlphaSettings AlphaSettings { get; private set; }

        public static int GetVertexSize(VertexFormat vertexFormat)
        {
            switch (vertexFormat)
            {
                case VertexFormat.Default:
                    return ByteHelper.GetSize(typeof(DefaultVertex.Data));
                case VertexFormat.Weighted:
                    return ByteHelper.GetSize(typeof(WeightedVertex.Data));
                case VertexFormat.Cinematic:
                    return ByteHelper.GetSize(typeof(CinematicVertex.Data));
            }

            throw new Exception($"Unkown vertex format - {vertexFormat}");
        }

        internal void SaveToByteArray(BinaryWriter writer, VertexFormat vertexFormat)
        {
            writer.Write(ByteHelper.GetBytes(AlphaSettings));
            for (int i = 0; i < VertexList.Length; i++)
            {
                if (vertexFormat == VertexFormat.Default)
                    writer.Write(ByteHelper.GetBytes(((DefaultVertex)VertexList[i])._data)); 
                else if (vertexFormat == VertexFormat.Cinematic)
                    writer.Write(ByteHelper.GetBytes(((CinematicVertex)VertexList[i])._data));
                else if (vertexFormat == VertexFormat.Weighted)
                    writer.Write(ByteHelper.GetBytes(((WeightedVertex)VertexList[i])._data));
                else
                    throw new Exception($"Unkown vertex format - {vertexFormat}");
            }

            for (int i = 0; i < IndexList.Length; i++)
                writer.Write(IndexList[i]);
        }

        public RmvMesh(byte[] data, VertexFormat vertexFormat, int vertexStart, uint vertexCount, int faceStart, uint faceCount)
        {
            //ref Point bytesAsPoint = ref Unsafe.As<byte, Point>(ref MemoryMarshal.GetReference(bytes));

            AlphaSettings = ByteHelper.ByteArrayToStructure<MeshAlphaSettings>(data, vertexStart - 8);
            var vertexSize = GetVertexSize(vertexFormat);

            VertexList = new BaseVertex[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                if (vertexFormat == VertexFormat.Default)
                {
                    var vertexData = ByteHelper.ByteArrayToStructure<DefaultVertex.Data>(data, vertexStart + i * vertexSize);
                    VertexList[i] = new DefaultVertex(vertexData);
                }
                else if (vertexFormat == VertexFormat.Cinematic)
                {
                    var vertexData = ByteHelper.ByteArrayToStructure<CinematicVertex.Data>(data, vertexStart + i * vertexSize);
                    VertexList[i] = new CinematicVertex(vertexData);
                }
                else if (vertexFormat == VertexFormat.Weighted)
                {
                    var vertexData = ByteHelper.ByteArrayToStructure<WeightedVertex.Data>(data, vertexStart + i * vertexSize);
                    VertexList[i] = new WeightedVertex(vertexData);
                }
                else
                {
                    throw new Exception($"Unkown vertex format - {vertexFormat}");
                }
            }

            IndexList = new ushort[faceCount];
            for (int i = 0; i < faceCount; i++)
                IndexList[i] =  BitConverter.ToUInt16(data, faceStart + sizeof(ushort) * i);
        }

        public RmvMesh(MeshAlphaSettings alphaSettings)
        {
            AlphaSettings = alphaSettings;
        }
    }

    public class RmvSubModel
    {
        int _modelStart;

        public RmvSubModelHeader Header { get; private set; }
        public List<RmvAttachmentPoint> AttachmentPoints;
        public List<RmvTexture> Textures;
        public RmvMesh Mesh { get; private set; }
        public string ParentSkeletonName { get; set; }    // Not part of the model definition

        public RmvSubModel(byte[] dataArray, int offset, string skeletonName)
        {
            _modelStart = offset;
            ParentSkeletonName = skeletonName;
            Header = LoadHeader(dataArray);
            AttachmentPoints = LoadAttachmentPoints(dataArray);
            Textures = LoadTextures(dataArray);
            Mesh = LoadMesh(dataArray);
        }

        public RmvSubModel()
        { }

        RmvSubModelHeader LoadHeader(byte[] dataArray)
        {
            return ByteHelper.ByteArrayToStructure<RmvSubModelHeader>(dataArray, _modelStart);
        }

        List<RmvAttachmentPoint> LoadAttachmentPoints(byte[] dataArray)
        {
            var attachmentPointStart = _modelStart + ByteHelper.GetSize<RmvSubModelHeader>();
            var attachmentPointSize = ByteHelper.GetSize<RmvAttachmentPoint>();

            var attachmentPoints = new List<RmvAttachmentPoint>();
            for (int i = 0; i < Header.AttachmentPointCount; i++)
            {
                var attachmentPoint = ByteHelper.ByteArrayToStructure<RmvAttachmentPoint>(dataArray, attachmentPointStart + attachmentPointSize * i);
                attachmentPoints.Add(attachmentPoint);
            }

            return attachmentPoints;
        }

        List<RmvTexture> LoadTextures(byte[] dataArray)
        {
            var attachmentsPointOffset = ByteHelper.GetSize<RmvAttachmentPoint>() * Header.AttachmentPointCount;
            var textureStart = _modelStart + ByteHelper.GetSize<RmvSubModelHeader>() + attachmentsPointOffset;
            var textureSize = ByteHelper.GetSize<RmvTexture>();

            var textures = new List<RmvTexture>();
            for (int i = 0; i < Header.TextureCount; i++)
            {
                var texture = ByteHelper.ByteArrayToStructure<RmvTexture>(dataArray, (int)textureStart + textureSize * i);
                textures.Add(texture);
            }

            return textures;
        }

        public RmvTexture GetTexture(TexureType texureType)
        {
            return Textures.FirstOrDefault(x => x.TexureType == texureType);
        }

        RmvMesh LoadMesh(byte[] dataArray)
        {
            var vertexStart =  Header.VertexOffset + _modelStart; 
            var faceStart = Header.FaceOffset + _modelStart;

            return new RmvMesh(dataArray, Header.VertextType, (int)vertexStart, Header.VertexCount, (int)faceStart, Header.FaceCount);
        }

       public RmvSubModel Clone(bool includeMesh = false)
       {       
           return new RmvSubModel()
           {
               _modelStart = _modelStart,
               ParentSkeletonName = ParentSkeletonName,
               Header = Header,
               AttachmentPoints = AttachmentPoints.Select(x => x).ToList(),
               Textures = Textures.Select(x => x).ToList(),
               Mesh = new RmvMesh(Mesh.AlphaSettings)
           };
       }
    }

   
}
