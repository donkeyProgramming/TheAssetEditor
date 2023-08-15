// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CommonControls.FileTypes.RigidModel.Transforms;
using CommonControls.FileTypes.RigidModel.Types;
using Filetypes.ByteParsing;
using Microsoft.Xna.Framework;

namespace CommonControls.FileTypes.RigidModel.MaterialHeaders
{
    public class WeightedMaterial : IMaterial
    {
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

                AttachmentPointParams = AttachmentPointParams.Select(x => x.Clone()).ToList(),
                TexturesParams = TexturesParams.Select(x => x.Clone()).ToList(),
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
                (4 * IntParams.Count) + (4 * IntParams.Count) +
                (4 * 4 * Vec4Params.Count) + (Vec4Params.Count * 4));

            return headerDataSize;
        }

        public RmvTexture? GetTexture(TextureType texureType)
        {
            if (TexturesParams.Count(x => x.TexureType == texureType) != 0)
                return TexturesParams.FirstOrDefault(x => x.TexureType == texureType);
            return null;
        }

        public List<RmvTexture> GetAllTextures()
        {
            return TexturesParams;
        }

        public void SetTexture(TextureType texureType, string path)
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
            BinaryVertexFormat = uiVertexFormat switch
            {
                UiVertexFormat.Cinematic => VertexFormat.Cinematic,
                UiVertexFormat.Weighted => VertexFormat.Weighted,
                UiVertexFormat.Static => VertexFormat.Static,
                _ => throw new Exception($"Unknown vertex type - {uiVertexFormat}"),
            };

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
                BinaryVertexFormat = (VertexFormat)Header._vertexType,
                ModelName = StringSanitizer.FixedString(Encoding.ASCII.GetString(Header._modelName)),
                Filters = StringSanitizer.FixedString(Encoding.ASCII.GetString(Header.Filters)),
                MatrixIndex = Header.MatrixIndex,
                ParentMatrixIndex = Header.ParentMatrixIndex,
                PivotPoint = Header.Transform.Pivot.ToVector3(),
                TextureDirectory = StringSanitizer.FixedString(Encoding.ASCII.GetString(Header._textureDir)),
                OriginalTransform = Header.Transform,
            };

            // Alpha mode
            material.AlphaMode = AlphaMode.Opaque;
            if (material.IntParams.Count != 0)
                material.AlphaMode = (AlphaMode)material.IntParams.First();

            return material;
        }

        public IMaterial CreateEmpty(ModelMaterialEnum materialId, RmvVersionEnum rmvType, VertexFormat vertexFormat)
        {
            var material = new WeightedMaterial()
            {
                MaterialId = materialId, 
                BinaryVertexFormat = vertexFormat,
                ModelName = "mesh1",
                TextureDirectory = "variantmeshes\\mesh1",
                OriginalTransform = new RmvTransform(),
                Filters = new string(""),
                PivotPoint = new Vector3(0, 0, 0),
                StringParams = new List<String>(),
                FloatParams = new List<float>(),
                Vec4Params = new List<RmvVector4>(),
                IntParams = new List<int>(),
                AttachmentPointParams = new List<RmvAttachmentPoint>(),
                TexturesParams = new List<RmvTexture>(),
                AlphaMode = AlphaMode.Transparent, /// Alpha mode - assume that users want transpencey mode enabled by default
            };

            return material;
        }

        public byte[] Save(IMaterial material)
        {
            if (material is not WeightedMaterial typedMaterial)
                throw new Exception("Incorrect material provided for WeightedMaterial::Save");

            // Create the header
            var header = new WeightedMaterialStruct()
            {
                _vertexType = (ushort)typedMaterial.BinaryVertexFormat,
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

            // Update alpha value in param list
            if (typedMaterial.IntParams.Count != 0)
                typedMaterial.IntParams[0] = (int)material.AlphaMode;

            for (var intIndex = 0; intIndex < typedMaterial.IntParams.Count; intIndex++)
            {
                writer.Write(ByteParsers.Int32.EncodeValue(intIndex, out _));
                typedMaterial.IntParams[0] = (int)material.AlphaMode;

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
