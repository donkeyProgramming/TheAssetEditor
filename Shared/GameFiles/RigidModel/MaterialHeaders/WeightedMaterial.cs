using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public class WeightedMaterial : IRmvMaterial
    {
        public enum MaterialHintEnum
        {
            None,
            Decal,
            Dirt,
            DecalAndDirt,
        }

        public UiVertexFormat ToolVertexFormat { get; set; }
        public MaterialHintEnum MaterialHint { get; set; } = MaterialHintEnum.None;

        // Actual attributes found in the rmv material
        public VertexFormat BinaryVertexFormat { get; set; } = VertexFormat.Unknown;
        public ModelMaterialEnum MaterialId { get; set; } = ModelMaterialEnum.weighted;
       
        public Vector3 PivotPoint { get; set; }
        public string ModelName { get; set; } = "";
        public string TextureDirectory { get; set; } = "";
        public string Filters { get; set; } = "";
        public RmvTransform OriginalTransform { get; set; }

        public int MatrixIndex { get; set; }
        public int ParentMatrixIndex { get; set; }

        public List<RmvAttachmentPoint> AttachmentPointParams { get; set; } = [];
        public List<RmvTexture> TexturesParams { get; set; } = [];
        public ParamList<string> StringParams { get; set; } = new ParamList<string>();
        public ParamList<float> FloatParams { get; set; } = new ParamList<float>();
        public ParamList<int> IntParams { get; set; } = new ParamList<int>();
        public ParamList<RmvVector4> Vec4Params { get; set; } = new ParamList<RmvVector4>();

        public IRmvMaterial Clone()
        {
            return new WeightedMaterial()
            {
                BinaryVertexFormat = BinaryVertexFormat,
                MatrixIndex = MatrixIndex,
                ParentMatrixIndex = ParentMatrixIndex,
                MaterialId = MaterialId,

                ModelName = ModelName,
                ToolVertexFormat = ToolVertexFormat,
                PivotPoint = PivotPoint,
                TextureDirectory = TextureDirectory,
                Filters = Filters,
                OriginalTransform = OriginalTransform,

                AttachmentPointParams = AttachmentPointParams.Select(x => x.Clone()).ToList(),
                TexturesParams = TexturesParams.Select(x => x.Clone()).ToList(),
                StringParams = StringParams.Clone(),
                FloatParams = FloatParams.Clone(),
                IntParams = IntParams.Clone(),
                Vec4Params = Vec4Params.Clone()
            };
        }

        public uint ComputeSize()
        {
            var stringParamSize = 0;
            foreach (var str in StringParams.Values)
            {
                var res = ByteParsers.String.Encode(str.Value, out _);
                stringParamSize += res.Length;
            }

            var headerDataSize = (uint)(ByteHelper.GetSize<WeightedMaterialStruct>() +
                ByteHelper.GetSize<RmvAttachmentPoint>() * AttachmentPointParams.Count +
                ByteHelper.GetSize<RmvTexture>() * TexturesParams.Count +
                stringParamSize +

                // Variable + index
                IntParams.GetByteSize(4) +
                FloatParams.GetByteSize(4) +
                Vec4Params.GetByteSize(4 * 4));

            return headerDataSize;
        }

        public RmvTexture? GetTexture(TextureType texureType)
        {
            if (TexturesParams.Any(x => x.TexureType == texureType))
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
                for (var i = 0; i < TexturesParams.Count; i++)
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
                for (var i = 0; i < TexturesParams.Count; i++)
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
                var newTexture = new RmvTexture
                {
                    TexureType = texureType,
                    Path = path
                };

                TexturesParams.Add(newTexture);
            }
        }

        public void UpdateInternalState(UiVertexFormat uiVertexFormat)
        {
            BinaryVertexFormat = uiVertexFormat switch
            {
                UiVertexFormat.Cinematic => VertexFormat.Cinematic,
                UiVertexFormat.Weighted => VertexFormat.Weighted,
                UiVertexFormat.Static => VertexFormat.Static,
                _ => throw new Exception($"Unknown vertex type - {uiVertexFormat}"),
            };

            //var hasDirt = IntParams.TryGet(WeightedParamterIds.IntParams_Dirt_index, out var dirtValue);
            //var hasDecal = IntParams.TryGet(WeightedParamterIds.IntParams_Dirt_index, out var decalValue);
            var isDirt = MaterialHint == MaterialHintEnum.Dirt || MaterialHint == MaterialHintEnum.DecalAndDirt;
            var isDecal = MaterialHint == MaterialHintEnum.Decal || MaterialHint == MaterialHintEnum.DecalAndDirt;

            // Overwrite the material type for static meshes
            MaterialId = ModelMaterialEnum.Unkown;
            if (BinaryVertexFormat == VertexFormat.Static)
            {
                MaterialId = ModelMaterialEnum.default_type;
                if(isDirt)
                    MaterialId = ModelMaterialEnum.dirtmap;
                if (isDecal)
                    MaterialId = ModelMaterialEnum.decal;
                if (isDirt && isDecal)
                    MaterialId = ModelMaterialEnum.decal_dirtmap;   
            }
            else
            {
                MaterialId = ModelMaterialEnum.weighted;
                if (isDirt)
                    MaterialId = ModelMaterialEnum.weighted_dirtmap;
                if (isDecal)
                    MaterialId = ModelMaterialEnum.weighted_decal;
                if (isDirt && isDecal)
                    MaterialId = ModelMaterialEnum.weighted_decal_dirtmap;
            }

            if (MaterialId == ModelMaterialEnum.Unkown)
                throw new Exception("Unable to determine vertex format.");
        }

        public void EnrichDataBeforeSaving(string[] boneNames)
        {
            AttachmentPointParams.Clear();
            for (var i = 0; i < boneNames.Length; i++)
            {
                var newPoint = new RmvAttachmentPoint
                {
                    BoneIndex = i,
                    Name = boneNames[i],
                    Matrix = RmvMatrix3x4.Identity()
                };
                AttachmentPointParams.Add(newPoint);
            }
        }
    }

    public class WeighterMaterialCreator : IMaterialCreator
    {
        public IRmvMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] dataArray, int dataOffset)
        {
            var header = ByteHelper.ByteArrayToStructure<WeightedMaterialStruct>(dataArray, dataOffset);
            dataOffset += ByteHelper.GetSize<WeightedMaterialStruct>();
            var material = new WeightedMaterial()
            {
                AttachmentPointParams = LoadAttachmentPoints(header.AttachmentPointCount, dataArray, ref dataOffset),
                TexturesParams = LoadTextures(header.TextureCount, dataArray, ref dataOffset),
                StringParams = ParamListHelper.LoadStringParams(header.StringParamCount, dataArray, ref dataOffset),
                FloatParams = ParamListHelper.LoadFloatParams(header.FloatParamCount, dataArray, ref dataOffset),
                IntParams = ParamListHelper.LoadIntParams(header.IntParamCount, dataArray, ref dataOffset),
                Vec4Params = ParamListHelper.LoadVec4Params(header.Vec4ParamCount, dataArray, ref dataOffset),

                MaterialId = materialId,
                BinaryVertexFormat = (VertexFormat)header._vertexType,
                ModelName = StringSanitizer.FixedString(Encoding.ASCII.GetString(header._modelName)),
                Filters = StringSanitizer.FixedString(Encoding.ASCII.GetString(header.Filters)),
                MatrixIndex = header.MatrixIndex,
                ParentMatrixIndex = header.ParentMatrixIndex,
                PivotPoint = header.Transform.Pivot.ToVector3(),
                TextureDirectory = StringSanitizer.FixedString(Encoding.ASCII.GetString(header._textureDir)),
                OriginalTransform = header.Transform,
            };

            return material;
        }

        public IRmvMaterial CreateEmpty(ModelMaterialEnum materialId)
        {
            var material = new WeightedMaterial()
            {
                MaterialId = materialId,
                BinaryVertexFormat = VertexFormat.Unknown,
                ModelName = "mesh1",
                TextureDirectory = "variantmeshes\\mesh1",
                OriginalTransform = new RmvTransform(),
                Filters = new string(""),
                PivotPoint = new Vector3(0, 0, 0),
                StringParams = new ParamList<string>(),
                FloatParams = new ParamList<float>(),
                Vec4Params = new ParamList<RmvVector4>(),
                IntParams = new ParamList<int>(),
                AttachmentPointParams = [],
                TexturesParams = [],
            };

            return material;
        }

        public byte[] Save(IRmvMaterial material)
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
                AttachmentPointCount = (uint)typedMaterial.AttachmentPointParams.Count,
                FloatParamCount = (uint)typedMaterial.FloatParams.Values.Count,
                IntParamCount = (uint)typedMaterial.IntParams.Values.Count,
                StringParamCount = (uint)typedMaterial.StringParams.Values.Count,
                Vec4ParamCount = (uint)typedMaterial.Vec4Params.Values.Count,
                TextureCount = (uint)typedMaterial.TexturesParams.Count,
                PaddingArray = new byte[124],
            };

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(ByteHelper.GetBytes(header));
            for (var attachmentPointIndex = 0; attachmentPointIndex < typedMaterial.AttachmentPointParams.Count; attachmentPointIndex++)
                writer.Write(ByteHelper.GetBytes(typedMaterial.AttachmentPointParams[attachmentPointIndex]));

            for (var textureIndex = 0; textureIndex < typedMaterial.TexturesParams.Count; textureIndex++)
                writer.Write(ByteHelper.GetBytes(typedMaterial.TexturesParams[textureIndex]));

            for (var stringIndex = 0; stringIndex < typedMaterial.StringParams.Values.Count; stringIndex++)
            {
                var param = typedMaterial.StringParams.Values[stringIndex];
                writer.Write(ByteParsers.Int32.EncodeValue(param.Index, out _));
                writer.Write(ByteParsers.String.Encode(param.Value, out _));
            }

            for (var floatIndex = 0; floatIndex < typedMaterial.FloatParams.Values.Count; floatIndex++)
            {
                var param = typedMaterial.IntParams.Values[floatIndex];
                writer.Write(ByteParsers.Int32.EncodeValue(param.Index, out _));
                writer.Write(ByteParsers.Single.EncodeValue(param.Value, out _));
            }

            for (var intIndex = 0; intIndex < typedMaterial.IntParams.Values.Count; intIndex++)
            {
                var param = typedMaterial.IntParams.Values[intIndex];
                writer.Write(ByteParsers.Int32.EncodeValue(param.Index, out _));
                writer.Write(ByteParsers.Int32.EncodeValue(param.Value, out _));
            }

            for (var vec4Index = 0; vec4Index < typedMaterial.Vec4Params.Values.Count; vec4Index++)
            {
                var param = typedMaterial.Vec4Params.Values[vec4Index];

                writer.Write(ByteParsers.Int32.EncodeValue(param.Index, out _));
                writer.Write(ByteParsers.Single.EncodeValue(param.Value.X, out _));
                writer.Write(ByteParsers.Single.EncodeValue(param.Value.Y, out _));
                writer.Write(ByteParsers.Single.EncodeValue(param.Value.Z, out _));
                writer.Write(ByteParsers.Single.EncodeValue(param.Value.W, out _));
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
            for (var i = 0; i < AttachmentPointCount; i++)
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
            for (var i = 0; i < TextureCount; i++)
            {
                var texture = ByteHelper.ByteArrayToStructure<RmvTexture>(dataArray, dataOffset);
                dataOffset += ByteHelper.GetSize<RmvTexture>();
                textures.Add(texture);
            }

            return textures;
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
