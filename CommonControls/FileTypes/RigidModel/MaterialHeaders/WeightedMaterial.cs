using CommonControls.Common;
using CommonControls.FileTypes;
using CommonControls.FileTypes.RigidModel.Transforms;
using CommonControls.FileTypes.RigidModel.Types;
using Filetypes.ByteParsing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CommonControls.FileTypes.RigidModel.MaterialHeaders
{
    public class WeightedMaterial : IMaterial
    {
        public UiVertexFormat VertexType { get; set; } = UiVertexFormat.Unknown;
        public VertexFormat BinaryVertexFormat { get; set; } = VertexFormat.Unknown;
        public Vector3 PivotPoint { get; set; }
        public AlphaMode AlphaMode { get; set; }
        public string ModelName { get; set; }

        public string TextureDirectory { get; set; }
        public string Filters { get; set; }
        public RmvTransform OriginalTransform { get; set; }

        public int MatrixIndex { get; set; }
        public int ParentMatrixIndex { get; set; }

        public List<RmvAttachmentPoint> AttachmentPointParams { get; set; }
        public List<RmvTexture> TexturesParams { get; set; }
        public List<string> StringParams { get; set; }
        public List<float> FloatParams { get; set; }
        public List<int> IntParams { get; set; }
        public List<RmvVector4> Vec4Params { get; set; }

        public ModelMaterialEnum MaterialId { get; set; } = ModelMaterialEnum.weighted;

        public IMaterial Clone()
        {
            return new WeightedMaterial()
            {
                VertexType = VertexType,
                BinaryVertexFormat = BinaryVertexFormat,
                MatrixIndex = MatrixIndex,
                ParentMatrixIndex = ParentMatrixIndex,
                MaterialId = MaterialId,

                ModelName = ModelName,
                AlphaMode = AlphaMode,
                PivotPoint = PivotPoint,
                TextureDirectory = TextureDirectory,
                Filters = Filters,
                OriginalTransform = OriginalTransform,

                AttachmentPointParams = AttachmentPointParams.Select(x => ObjectHelper.DeepClone(x)).ToList(),
                TexturesParams = TexturesParams.Select(x => ObjectHelper.DeepClone(x)).ToList(),
                StringParams = StringParams.Select(x => x).ToList(),
                FloatParams = FloatParams.Select(x => x).ToList(),
                IntParams = IntParams.Select(x => x).ToList(),
                Vec4Params = Vec4Params.Select(x => x).ToList(),
            };
        }

        void UpdateAttachmentPointList(string[] boneNames)
        {
            AttachmentPointParams.Clear();
            for (int i = 0; i < boneNames.Length; i++)
            {
                var a = new RmvAttachmentPoint
                {
                    BoneIndex = i,
                    Name = boneNames[i],
                    Matrix = RmvMatrix3x4.Identity()
                };
                AttachmentPointParams.Add(a);
            }
        }

        public uint ComputeSize()
        {
            var stringParamSize = 0;
            foreach (var str in StringParams)
            {
                var res = ByteParsers.String.Encode(str, out _);
                stringParamSize += res.Length;
            }

            var headerDataSize = (uint)(ByteHelper.GetSize<WeightedMaterialStruct>() +
                ByteHelper.GetSize<RmvAttachmentPoint>() * AttachmentPointParams.Count +
                ByteHelper.GetSize<RmvTexture>() * TexturesParams.Count +
                stringParamSize +
                // Variable + index
                (4 * FloatParams.Count) + (4 * FloatParams.Count) +
                (4 * IntParams.Count) + (4  * IntParams.Count) +
                (4 * 4 * Vec4Params.Count) + (Vec4Params.Count * 4));

            return headerDataSize;
        }

        public RmvTexture? GetTexture(TexureType texureType)
        {
            if (TexturesParams.Count(x => x.TexureType == texureType) != 0)
                return TexturesParams.FirstOrDefault(x => x.TexureType == texureType);
            return null;
        }

        public List<RmvTexture> GetAllTextures()
        {
            return TexturesParams;
        }

        public void SetTexture(TexureType texureType, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                // Delete texture
                for (int i = 0; i < TexturesParams.Count; i++)
                {
                    if (TexturesParams[i].TexureType == texureType)
                    {
                        TexturesParams.RemoveAt(i);
                        return;
                    }
                }
            }
            else
            {
                // Update texture
                for (int i = 0; i < TexturesParams.Count; i++)
                {
                    if (TexturesParams[i].TexureType == texureType)
                    {
                        var item = TexturesParams[i];
                        item.Path = path;
                        TexturesParams[i] = item;
                        return;
                    }
                }

                // Add if missing
                RmvTexture newTexture = new RmvTexture();
                newTexture.TexureType = texureType;
                newTexture.Path = path;

                TexturesParams.Add(newTexture);
            }
        }

        public void UpdateEnumsBeforeSaving(UiVertexFormat uiVertexFormat, RmvVersionEnum outputVersion)
        {
            if (outputVersion == RmvVersionEnum.RMV2_V8)
            {
                if (uiVertexFormat == UiVertexFormat.Cinematic)
                {
                    VertexType = UiVertexFormat.Cinematic;
                    BinaryVertexFormat = VertexFormat.Cinematic_withTint;
                }
                else if (uiVertexFormat == UiVertexFormat.Weighted)
                {
                    VertexType = UiVertexFormat.Weighted;
                    BinaryVertexFormat = VertexFormat.Weighted_withTint;
                }
                else
                {
                    VertexType = UiVertexFormat.Static;
                    BinaryVertexFormat = VertexFormat.Static;
                }
            }
            else
            {
                if (uiVertexFormat == UiVertexFormat.Cinematic)
                {
                    VertexType = UiVertexFormat.Cinematic;
                    BinaryVertexFormat = VertexFormat.Cinematic;
                }
                else if (uiVertexFormat == UiVertexFormat.Weighted)
                {
                    VertexType = UiVertexFormat.Weighted;
                    BinaryVertexFormat = VertexFormat.Weighted;
                }
                else
                {
                    VertexType = UiVertexFormat.Static;
                    BinaryVertexFormat = VertexFormat.Static;
                }
            }

            // Overwrite the material type for static meshes
            if (BinaryVertexFormat == VertexFormat.Static)
                MaterialId = ModelMaterialEnum.default_type;
            else
                MaterialId = ModelMaterialEnum.weighted;


        }

        public void EnrichDataBeforeSaving(string[] boneNames, BoundingBox boundingBox)
        {
            UpdateAttachmentPointList(boneNames);
        }
    }

    public class WeighterMaterialCreator : IMaterialCreator
    {
        public IMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] dataArray, int dataOffset)
        {
            var Header = ByteHelper.ByteArrayToStructure<WeightedMaterialStruct>(dataArray, dataOffset);
            dataOffset += ByteHelper.GetSize<WeightedMaterialStruct>();
            var material = new WeightedMaterial()
            {
                AttachmentPointParams = LoadAttachmentPoints(Header.AttachmentPointCount, dataArray, ref dataOffset),
                TexturesParams = LoadTextures(Header.TextureCount, dataArray, ref dataOffset),
                StringParams = LoadStringParams(Header.StringParamCount, dataArray, ref dataOffset),
                FloatParams = LoadFloatParams(Header.FloatParamCount, dataArray, ref dataOffset),
                IntParams = LoadIntParams(Header.IntParamCount, dataArray, ref dataOffset),
                Vec4Params = LoadVec4Params(Header.Vec4ParamCount, dataArray, ref dataOffset),

                MaterialId = materialId,
                VertexType = (UiVertexFormat)Header._vertexType,
                BinaryVertexFormat = (VertexFormat)Header._vertexType,
                ModelName = Util.SanatizeFixedString(Encoding.ASCII.GetString(Header._modelName)),
                Filters = Util.SanatizeFixedString(Encoding.ASCII.GetString(Header.Filters)),
                MatrixIndex = Header.MatrixIndex,
                ParentMatrixIndex = Header.ParentMatrixIndex,
                PivotPoint = Header.Transform.Pivot.ToVector3(),
                TextureDirectory = Util.SanatizeFixedString(Encoding.ASCII.GetString(Header._textureDir)),
                OriginalTransform = Header.Transform,
            };

            // Alpha mode
            material.AlphaMode = AlphaMode.Opaque;
            if (material.IntParams.Count != 0)
                material.AlphaMode = (AlphaMode)material.IntParams.First();

            // Version 8 fix for different vertex format with same id! 
            if (rmvType == RmvVersionEnum.RMV2_V8)
            {
                if (material.BinaryVertexFormat == VertexFormat.Weighted)
                    material.BinaryVertexFormat = VertexFormat.Weighted_withTint;
                else if (material.BinaryVertexFormat == VertexFormat.Cinematic)
                    material.BinaryVertexFormat = VertexFormat.Cinematic_withTint;
                else if (material.BinaryVertexFormat == VertexFormat.Static)
                    material.BinaryVertexFormat = VertexFormat.Static;
                else
                    throw new Exception("Unknown vertex format for material");
            }

            return material;
        }

        public byte[] Save(IMaterial material)
        {
            var typedMaterial = material as WeightedMaterial;
            if (typedMaterial == null)
                throw new Exception("Incorrect material provided for WeightedMaterial::Save");

            // Create the header
            var header = new WeightedMaterialStruct()
            {
                _vertexType = (ushort)typedMaterial.VertexType,
                _modelName = ByteHelper.CreateFixLengthString(typedMaterial.ModelName, 32),
                _textureDir = ByteHelper.CreateFixLengthString(typedMaterial.TextureDirectory, 256),
                Filters = ByteHelper.CreateFixLengthString(typedMaterial.Filters, 256),
                Transform = new RmvTransform()
                {
                    Pivot = new RmvVector3(typedMaterial.PivotPoint),
                    Matrix0 = typedMaterial.OriginalTransform.Matrix0,
                    Matrix1 = typedMaterial.OriginalTransform.Matrix1,
                    Matrix2 = typedMaterial.OriginalTransform.Matrix2
                },
                MatrixIndex = typedMaterial.MatrixIndex,
                ParentMatrixIndex = typedMaterial.ParentMatrixIndex,
                AttachmentPointCount = (uint)typedMaterial.AttachmentPointParams.Count(),
                FloatParamCount = (uint)typedMaterial.FloatParams.Count(),
                IntParamCount = (uint)typedMaterial.IntParams.Count(),
                StringParamCount = (uint)typedMaterial.StringParams.Count(),
                Vec4ParamCount = (uint)typedMaterial.Vec4Params.Count(),
                TextureCount = (uint)typedMaterial.TexturesParams.Count(),
                PaddingArray = new byte[124],
            };

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(ByteHelper.GetBytes(header));
            for (var attachmentPointIndex = 0; attachmentPointIndex < typedMaterial.AttachmentPointParams.Count; attachmentPointIndex++)
                writer.Write(ByteHelper.GetBytes(typedMaterial.AttachmentPointParams[attachmentPointIndex]));

            for (var textureIndex = 0; textureIndex < typedMaterial.TexturesParams.Count; textureIndex++)
                writer.Write(ByteHelper.GetBytes(typedMaterial.TexturesParams[textureIndex]));

            for (var stringIndex = 0; stringIndex < typedMaterial.StringParams.Count; stringIndex++)
            {
                writer.Write(ByteParsers.Int32.EncodeValue(stringIndex, out _));
                writer.Write(ByteParsers.String.Encode(typedMaterial.StringParams[stringIndex], out _));
            }

            for (var floatIndex = 0; floatIndex < typedMaterial.FloatParams.Count; floatIndex++)
            {
                writer.Write(ByteParsers.Int32.EncodeValue(floatIndex, out _));
                writer.Write(ByteParsers.Single.EncodeValue(typedMaterial.FloatParams[floatIndex], out _));
            }

            for (var intIndex = 0; intIndex < typedMaterial.IntParams.Count; intIndex++)
            {
                writer.Write(ByteParsers.Int32.EncodeValue(intIndex, out _));
                writer.Write(ByteParsers.Int32.EncodeValue(typedMaterial.IntParams[intIndex], out _));
            }

            for (var vec4Index = 0; vec4Index < typedMaterial.Vec4Params.Count; vec4Index++)
            {
                writer.Write(ByteParsers.Int32.EncodeValue(vec4Index, out _));
                writer.Write(ByteParsers.Single.EncodeValue(typedMaterial.Vec4Params[vec4Index].X, out _));
                writer.Write(ByteParsers.Single.EncodeValue(typedMaterial.Vec4Params[vec4Index].Y, out _));
                writer.Write(ByteParsers.Single.EncodeValue(typedMaterial.Vec4Params[vec4Index].Z, out _));
                writer.Write(ByteParsers.Single.EncodeValue(typedMaterial.Vec4Params[vec4Index].W, out _));
            }

            var bytes = ms.ToArray();
            var expectedSize = material.ComputeSize();
            if (bytes.Length != expectedSize)
                throw new Exception("Difference in actual and expected byte size for weighted material");

            return bytes;
        }

        List<RmvAttachmentPoint> LoadAttachmentPoints(uint AttachmentPointCount, byte[] dataArray, ref int dataOffset)
        {
            var attachmentPoints = new List<RmvAttachmentPoint>();
            for (int i = 0; i < AttachmentPointCount; i++)
            {
                var attachmentPoint = ByteHelper.ByteArrayToStructure<RmvAttachmentPoint>(dataArray, dataOffset);
                dataOffset += ByteHelper.GetSize<RmvAttachmentPoint>();
                attachmentPoints.Add(attachmentPoint);
            }

            return attachmentPoints;
        }

        List<RmvTexture> LoadTextures(uint TextureCount, byte[] dataArray, ref int dataOffset)
        {
            var textures = new List<RmvTexture>();
            for (int i = 0; i < TextureCount; i++)
            {
                var texture = ByteHelper.ByteArrayToStructure<RmvTexture>(dataArray, dataOffset);
                dataOffset += ByteHelper.GetSize<RmvTexture>();
                textures.Add(texture);
            }

            return textures;
        }
        List<string> LoadStringParams(uint StringParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new List<string>();
            for (int i = 0; i < StringParamCount; i++)
            {
                var index = ByteParsers.UInt32.TryDecode(dataArray, dataOffset, out _, out _, out _);
                var result = ByteParsers.String.TryDecode(dataArray, dataOffset + 4, out var value, out var byteLength, out var error);
                if (!result)
                {
                    throw new Exception("Error reading string parameter - " + error);
                }
                dataOffset += byteLength + 4;
                output.Add(value);
            }
            return output;
        }


        List<float> LoadFloatParams(uint FloatParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new List<float>();
            for (int i = 0; i < FloatParamCount; i++)
            {
                var index = ByteParsers.UInt32.TryDecode(dataArray, dataOffset, out _, out _, out _);
                var result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 4, out var value, out var byteLength, out var error);
                if (!result)
                {
                    throw new Exception("Error reading float parameter - " + error);
                }
                dataOffset += byteLength + 4;
                output.Add(value);
            }
            return output;
        }

        List<int> LoadIntParams(uint IntParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new List<int>();
            for (int i = 0; i < IntParamCount; i++)
            {
                var index = ByteParsers.UInt32.TryDecode(dataArray, dataOffset, out _, out _, out _);
                var result = ByteParsers.Int32.TryDecodeValue(dataArray, dataOffset + 4, out var value, out var byteLength, out var error);
                if (!result)
                {
                    throw new Exception("Error reading int parameter - " + error);
                }
                dataOffset += byteLength + 4;
                output.Add(value);
            }
            return output;
        }

        List<RmvVector4> LoadVec4Params(uint Vec4ParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new List<RmvVector4>();
            for (int i = 0; i < Vec4ParamCount; i++)
            {
                var index = ByteParsers.UInt32.TryDecode(dataArray, dataOffset, out _, out _, out _);

                var result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 4, out var x, out var byteLength, out var error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 8, out var y, out byteLength, out error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 12, out var z, out byteLength, out error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 16, out var w, out byteLength, out error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                dataOffset += 4 * 4 + 4;
                output.Add(new RmvVector4(x, y, z, w));
            }
            return output;
        }

    }

    struct WeightedMaterialStruct
    {
        public ushort _vertexType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] _modelName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] _textureDir;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Filters;  // Always zero?

        public byte PaddingByte0;
        public byte PaddingByte1;

        public RmvTransform Transform;

        public int MatrixIndex;         // Used when meshes should be attached to a bone, eg for buildings being destroyed 
        public int ParentMatrixIndex;   // Always -1 it seems
        public uint AttachmentPointCount;
        public uint TextureCount;
        public uint StringParamCount;
        public uint FloatParamCount;
        public uint IntParamCount;
        public uint Vec4ParamCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 124)]
        public byte[] PaddingArray;
    }
}