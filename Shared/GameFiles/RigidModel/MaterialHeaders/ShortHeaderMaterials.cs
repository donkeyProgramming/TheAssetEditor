using System.Text;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    // Materials with a short header of their own instead of the weighted layout. Their ids differ between games,
    // so ShortHeaderMaterials.TryCreate picks a family by id and header size.
    // Layouts: rmv2_anim_blender_import_export, docs/FORMATS.md "The short material headers".

    /// <summary>A zero-padded fixed-width UTF-8 string. Keeps CA's bytes after the terminator, so an unchanged value saves as read.</summary>
    public class FixedWidthString
    {
        readonly byte[] _raw;
        public int Width { get; }
        public string Value { get; set; }

        public FixedWidthString(int width, string value = "")
        {
            Width = width;
            Value = value;
            _raw = Encode(value, width);
        }

        public FixedWidthString(byte[] data, int offset, int width)
        {
            Width = width;
            _raw = data.AsSpan(offset, width).ToArray();
            Value = StringSanitizer.FixedString(Encoding.UTF8.GetString(_raw));
        }

        public byte[] ToBytes() => StringSanitizer.FixedString(Encoding.UTF8.GetString(_raw)) == Value ? _raw.ToArray() : Encode(Value, Width);

        public FixedWidthString Clone()
        {
            var output = new FixedWidthString(_raw, 0, Width);
            output.Value = Value;
            return output;
        }

        static byte[] Encode(string value, int width)
        {
            var output = new byte[width];
            var bytes = Encoding.UTF8.GetBytes(value);
            Array.Copy(bytes, output, Math.Min(bytes.Length, width));
            return output;
        }
    }

    public abstract class ShortHeaderMaterial : IRmvMaterial
    {
        public ModelMaterialEnum MaterialId { get; set; }

        // None of these headers has a vertex-format field. Decals and terrain tiles use a float4 position.
        public VertexFormat BinaryVertexFormat { get; set; } = VertexFormat.Unknown;
        public virtual Vector3 PivotPoint { get; set; } = Vector3.Zero;
        public virtual string ModelName { get; set; } = "";
        public virtual string TextureDirectory { get => ""; set { } }

        public abstract IRmvMaterial Clone();
        public abstract uint ComputeSize();
        public abstract byte[] Save();

        public virtual List<RmvTexture> GetAllTextures() => [];
        public RmvTexture? GetTexture(TextureType texureType) => null;
        public void SetTexture(TextureType texureType, string path) { }

        public void UpdateInternalState(UiVertexFormat uiVertexFormat) => throw new NotSupportedException($"{GetType().Name} ({MaterialId}) can not be rebuilt from an edited mesh");
        public void EnrichDataBeforeSaving(List<RmvAttachmentPoint> attachmentPoints, int matrixOverride) { }

        protected static uint[] ReadUInts(byte[] data, int offset, int count) => Enumerable.Range(0, count).Select(i => BitConverter.ToUInt32(data, offset + i * 4)).ToArray();
        protected static float[] ReadFloats(byte[] data, int offset, int count) => Enumerable.Range(0, count).Select(i => BitConverter.ToSingle(data, offset + i * 4)).ToArray();
    }

    /// <summary>
    /// A projected decal (ids 67, 87, 95, 100): a 256-byte texture path, then 1, 9, 10 or 0 floats of unclear
    /// meaning. Warhammer 3's decals have no vertex block, only the 36 indices of a unit cube.
    /// </summary>
    public class DecalMaterial : ShortHeaderMaterial
    {
        public const int PathWidth = 256;
        public FixedWidthString TexturePath { get; set; } = new(PathWidth);
        public float[] Values { get; set; } = [];

        public override string TextureDirectory { get => TexturePath.Value; set => TexturePath.Value = value; }

        public override uint ComputeSize() => (uint)(PathWidth + Values.Length * 4);

        public override byte[] Save() => [.. TexturePath.ToBytes(), .. Values.SelectMany(BitConverter.GetBytes)];

        public override IRmvMaterial Clone() => new DecalMaterial { MaterialId = MaterialId, BinaryVertexFormat = BinaryVertexFormat, PivotPoint = PivotPoint, ModelName = ModelName, TexturePath = TexturePath.Clone(), Values = Values.ToArray() };

        public static DecalMaterial Load(ModelMaterialEnum materialId, byte[] data, int offset, long size) => new()
        {
            MaterialId = materialId,
            BinaryVertexFormat = VertexFormat.Position16_bit,
            TexturePath = new FixedWidthString(data, offset, PathWidth),
            Values = ReadFloats(data, offset + PathWidth, (int)(size - PathWidth) / 4),
        };
    }

    /// <summary>Terrain tiles: a 64-byte name (TerrainBase0), then five (id 66) or six unknown words.</summary>
    public class TerrainTileMaterial : ShortHeaderMaterial
    {
        public const int NameWidth = 64;
        public FixedWidthString Name { get; set; } = new(NameWidth, "TerrainBase0");
        public uint[] Values { get; set; } = new uint[6];

        public override string ModelName { get => Name.Value; set => Name.Value = value; }

        public override uint ComputeSize() => (uint)(NameWidth + Values.Length * 4);

        public override byte[] Save() => [.. Name.ToBytes(), .. Values.SelectMany(BitConverter.GetBytes)];

        public override IRmvMaterial Clone() => new TerrainTileMaterial { MaterialId = MaterialId, BinaryVertexFormat = BinaryVertexFormat, PivotPoint = PivotPoint, Name = Name.Clone(), Values = Values.ToArray() };

        public static TerrainTileMaterial Load(ModelMaterialEnum materialId, byte[] data, int offset, long size) => new()
        {
            MaterialId = materialId,
            BinaryVertexFormat = VertexFormat.Position16_bit,
            Name = new FixedWidthString(data, offset, NameWidth),
            Values = ReadUInts(data, offset + NameWidth, (int)(size - NameWidth) / 4),
        };
    }

    /// <summary>3D interface banners (ids 29, 30): a 256-byte model name, then eight words that are always zero.</summary>
    public class BannerMaterial : ShortHeaderMaterial
    {
        public const int NameWidth = 256;
        public FixedWidthString Name { get; set; } = new(NameWidth);
        public uint[] Values { get; set; } = new uint[8];

        public override string ModelName { get => Name.Value; set => Name.Value = value; }

        public override uint ComputeSize() => (uint)(NameWidth + Values.Length * 4);

        public override byte[] Save() => [.. Name.ToBytes(), .. Values.SelectMany(BitConverter.GetBytes)];

        public override IRmvMaterial Clone() => new BannerMaterial { MaterialId = MaterialId, BinaryVertexFormat = BinaryVertexFormat, PivotPoint = PivotPoint, Name = Name.Clone(), Values = Values.ToArray() };

        public static BannerMaterial Load(ModelMaterialEnum materialId, byte[] data, int offset, long size) => new()
        {
            MaterialId = materialId,
            Name = new FixedWidthString(data, offset, NameWidth),
            Values = ReadUInts(data, offset + NameWidth, (int)(size - NameWidth) / 4),
        };
    }

    /// <summary>
    /// A static point light (id 45): a 256-byte preset name, a second 256-byte field (always empty) and the
    /// light's position, read as the pivot. The mesh has indices but no vertices.
    /// </summary>
    public class StaticPointLightMaterial : ShortHeaderMaterial
    {
        public const int NameWidth = 256;
        public const int Size = NameWidth * 2 + 12;
        public FixedWidthString Name { get; set; } = new(NameWidth);
        public FixedWidthString SecondName { get; set; } = new(NameWidth);

        public override string ModelName { get => Name.Value; set => Name.Value = value; }

        public override uint ComputeSize() => Size;

        public override byte[] Save() => [.. Name.ToBytes(), .. SecondName.ToBytes(), .. BitConverter.GetBytes(PivotPoint.X), .. BitConverter.GetBytes(PivotPoint.Y), .. BitConverter.GetBytes(PivotPoint.Z)];

        public override IRmvMaterial Clone() => new StaticPointLightMaterial { MaterialId = MaterialId, BinaryVertexFormat = BinaryVertexFormat, PivotPoint = PivotPoint, Name = Name.Clone(), SecondName = SecondName.Clone() };

        public static StaticPointLightMaterial Load(ModelMaterialEnum materialId, byte[] data, int offset)
        {
            var position = ReadFloats(data, offset + NameWidth * 2, 3);
            return new()
            {
                MaterialId = materialId,
                Name = new FixedWidthString(data, offset, NameWidth),
                SecondName = new FixedWidthString(data, offset + NameWidth, NameWidth),
                PivotPoint = new Vector3(position[0], position[1], position[2]),
            };
        }
    }

    /// <summary>No material header at all (Rome 2-era bow waves).</summary>
    public class NoHeaderMaterial : ShortHeaderMaterial
    {
        public override uint ComputeSize() => 0;
        public override byte[] Save() => [];
        public override IRmvMaterial Clone() => new NoHeaderMaterial { MaterialId = MaterialId, BinaryVertexFormat = BinaryVertexFormat, PivotPoint = PivotPoint, ModelName = ModelName };
    }

    public static class ShortHeaderMaterials
    {
        static readonly ModelMaterialEnum[] s_terrainTileIds = [ModelMaterialEnum.TerrainTiles, (ModelMaterialEnum)66, ModelMaterialEnum.weighted_texture_blend, ModelMaterialEnum.projected_decal_v4];
        static readonly ModelMaterialEnum[] s_decalIds = [ModelMaterialEnum.projected_decal, ModelMaterialEnum.projected_decal_v2, ModelMaterialEnum.projected_decal_v3, ModelMaterialEnum.alpha_blend];
        static readonly ModelMaterialEnum[] s_bannerIds = [ModelMaterialEnum.texture_combo_vertex_wind, ModelMaterialEnum.texture_combo];

        // Only when id and size agree: these ids can also carry a full weighted header (Warhammer's 97 on swaying props)
        public static bool TryCreate(ModelMaterialEnum materialId, byte[] data, int offset, long size, out ShortHeaderMaterial material)
        {
            material = null;
            if (size == 0)
                material = new NoHeaderMaterial { MaterialId = materialId };
            else if (s_terrainTileIds.Contains(materialId) && (size == TerrainTileMaterial.NameWidth + 5 * 4 || size == TerrainTileMaterial.NameWidth + 6 * 4))
                material = TerrainTileMaterial.Load(materialId, data, offset, size);
            else if (s_bannerIds.Contains(materialId) && IsNamePlusWords(size, BannerMaterial.NameWidth))
                material = BannerMaterial.Load(materialId, data, offset, size);
            else if (materialId == ModelMaterialEnum.static_point_light && size == StaticPointLightMaterial.Size)
                material = StaticPointLightMaterial.Load(materialId, data, offset);
            else if (s_decalIds.Contains(materialId) && IsNamePlusWords(size, DecalMaterial.PathWidth))
                material = DecalMaterial.Load(materialId, data, offset, size);

            return material != null;
        }

        static bool IsNamePlusWords(long size, int nameWidth) => size >= nameWidth && (size - nameWidth) % 4 == 0 && size - nameWidth <= 64;
    }
}
