using System.Text;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    /*
     * The material families that use a short header of their own instead of the weighted layout.
     * Ids are not stable across the series (terrain tiles are 66, 96 or 97 in the Rome 2 era and
     * 101 in Warhammer; decals run 67, 87, 95 and 100), so a family is picked by id AND by the
     * header size its section implies - see ShortHeaderMaterials.TryCreate.
     *
     * Every layout here and its corpus evidence comes from rmv2_anim_blender_import_export,
     * docs/FORMATS.md "The short material headers". Fixed-width strings keep the bytes CA left
     * after the terminator, so an unchanged name is written back exactly as it was read.
     */

    /// <summary>A zero-padded fixed-width UTF-8 string that remembers its original bytes.</summary>
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

        /// <summary>
        /// None of these headers has a vertex-format field. Decals and terrain tiles carry a
        /// float4 position (stride 16); a mesh whose stride does not match is kept as raw bytes.
        /// </summary>
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
    /// A projected decal (ids 67, 87, 95, 100): a 256-byte texture path, then a run of floats
    /// whose count grows with the id - one for 67, nine for 87 (two vec3 that read as placement
    /// and extent, then three zeros), ten for 95 (the last an angle), none for Warhammer 3's 100.
    /// The layout is certain; what the floats mean is not. Warhammer 3's decals ship no vertex
    /// block: the mesh is a unit cube (-0.5 to 0.5) whose 24 vertices the game supplies, and the
    /// file holds only its 36 indices.
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

    /// <summary>
    /// The terrain-tile material: a 64-byte name (TerrainBase0) and five or six words of unknown
    /// meaning. Warhammer's id 101 carries six; Rome 2 and Attila ship id 66 with five and ids 96
    /// and 97 with six.
    /// </summary>
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

    /// <summary>
    /// The 3D interface banners (ids 29 and 30): a 256-byte model name such as
    /// "army_banner_strength_fill_lod1", then eight words that are zero in every observed file.
    /// </summary>
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
    /// A static point light (id 45): a 256-byte light preset name ("orange_10m_glow"), a second
    /// 256-byte field that is empty in every observed file, and the light's position in model
    /// space, read as the pivot. Like Warhammer 3's decals the mesh ships indices but no vertices.
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

    /// <summary>
    /// No header at all: the section goes straight from the common header to the vertices, so
    /// there is no vertex-format field either. Bow waves do this in the Rome 2 era.
    /// </summary>
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

        /// <summary>
        /// Reads a short header when the id and the section's header size both say it is one.
        /// Anything else, including these ids carrying a full weighted header (Warhammer's id 97
        /// on wind-swayed props, for one), is left to the weighted layout.
        /// </summary>
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
