using Filetypes.ByteParsing;
using Filetypes.RigidModel.Transforms;
using Filetypes.RigidModel.Vertex;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        byte _unknown0;
        byte _unknown1;

        public RmvTransform Transform;

        public int LinkDirectlyToBoneIndex;
        public int Flag_alwaysNegativeOne;
        public uint AttachmentPointCount;
        public uint TextureCount;

        UknownData _uknownData;


        public VertexFormat VertextType
        {
            get => (VertexFormat)_vertexType;
        }

        public string ModelName
        {
            get
            {
                var result = ByteParsers.String.TryDecodeFixedLength(_modelName, 0, 32, out string value, out _);
                if (result == false)
                    throw new Exception();
                return Util.SanatizeFixedString(value);
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
    }

    [StructLayout(LayoutKind.Sequential)]
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
        public BaseVertex[] VertexList { get; private set; }
        public ushort[] _indexList;
        public MeshAlphaSettings AlphaSettings { get; private set; }

        int GetVertexSize(VertexFormat vertexFormat)
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

            _indexList = new ushort[faceCount];
            for (int i = 0; i < faceCount; i++)
                _indexList[i] =  BitConverter.ToUInt16(data, faceStart + sizeof(ushort) * i);
        }
    }

    public class RmvSubModel
    {
        int _modelStart;

        public RmvSubModelHeader Header { get; private set; }

        public List<RmvAttachmentPoint> AttachmentPoints;
        public List<RmvTexture> Textures;

        public RmvMesh Mesh { get; private set; }


        public RmvSubModel(byte[] dataArray, int offset)
        {
            _modelStart = offset;
     

            Header = LoadHeader(dataArray);
            AttachmentPoints = LoadAttachmentPoints(dataArray);
            Textures = LoadTextures(dataArray);
            Mesh = LoadMesh(dataArray);
        }

        RmvSubModelHeader LoadHeader(byte[] dataArray)
        {
            return ByteHelper.ByteArrayToStructure<RmvSubModelHeader>(dataArray, _modelStart);
        }

        List<RmvAttachmentPoint> LoadAttachmentPoints(byte[] dataArray)
        {
            var attachmentPointStart = _modelStart + Marshal.SizeOf(typeof(RmvSubModelHeader));
            var attachmentPointSize = Marshal.SizeOf(typeof(RmvAttachmentPoint));

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
            var attachmentsPointOffset = Marshal.SizeOf(typeof(RmvAttachmentPoint)) * Header.AttachmentPointCount;
            var textureStart = _modelStart + Marshal.SizeOf(typeof(RmvSubModelHeader)) + attachmentsPointOffset;
            var textureSize = Marshal.SizeOf(typeof(RmvTexture));

            var textures = new List<RmvTexture>();
            for (int i = 0; i < Header.TextureCount; i++)
            {
                var texture = ByteHelper.ByteArrayToStructure<RmvTexture>(dataArray, (int)textureStart + textureSize * i);
                textures.Add(texture);
            }

            return textures;
        }


        RmvMesh LoadMesh(byte[] dataArray)
        {
            var vertexStart =  Header.VertexOffset + _modelStart; 
            var faceStart = Header.FaceOffset + _modelStart;
            /*
            var s0 = ByteHelper.GetSize(typeof(RmvMeshHeader));
            var s1 = ByteHelper.GetSize(typeof(RmvTexture)) * Header.TextureCount;
            var s2 = ByteHelper.GetSize(typeof(RmvAttachmentPoint)) *Header.AttachmentPointCount;
            */
            return new RmvMesh(dataArray, Header.VertextType, (int)vertexStart, Header.VertexCount, (int)faceStart, Header.FaceCount);
        }
    }




    class ByteHelper
    {
        public static T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                var p = handle.AddrOfPinnedObject() + offset;
                return (T)Marshal.PtrToStructure(p, typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static int GetSize(Type type)
        { 
         return Marshal.SizeOf(type) ;
        }
    }
}
