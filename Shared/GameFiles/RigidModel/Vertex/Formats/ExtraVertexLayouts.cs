using Microsoft.Xna.Framework;
using Half = SharpDX.Half;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    /*
     * The vertex layouts that the material's declared format id does not identify on its own.
     * Several ids cover more than one layout and the game tells them apart by stride, so a
     * layout is resolved from the pair (declared id, stride); see VertexFactory.ResolveLayout.
     * The file's declared id stays on the material and is written back unchanged.
     *
     * Channels with no field in CommonVertex ride in CommonVertex.Extra, in the order listed on
     * each layout, so the mesh saves back as it was read.
     *
     * Layouts and their evidence: rmv2_anim_blender_import_export, docs/FORMATS.md "Vertex layouts".
     */
    public abstract class ExtraVertexLayout : IVertexCreator
    {
        public abstract VertexFormat Type { get; }
        public abstract string Name { get; }
        protected abstract int Stride { get; }
        public bool ForceComputeNormals => false;

        public uint GetVertexSize(RmvVersionEnum rmvVersion) => (uint)Stride;

        public CommonVertex[] ReadArray(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize, int vertexCount)
        {
            using var reader = new BinaryReader(new MemoryStream(buffer, offset, vertexSize * vertexCount));
            var output = new CommonVertex[vertexCount];
            for (var i = 0; i < vertexCount; i++)
            {
                output[i] = new CommonVertex { BoneIndex = [], BoneWeight = [], Normal = Vector3.UnitY, Tangent = Vector3.UnitX, BiNormal = Vector3.UnitZ };
                Read(reader, output[i]);
            }
            return output;
        }

        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            Write(writer, vertex, rmvVersion);
            var bytes = stream.ToArray();
            if (bytes.Length != Stride)
                throw new Exception($"{Name} vertex wrote {bytes.Length} bytes, expected {Stride}");
            return bytes;
        }

        protected abstract void Read(BinaryReader reader, CommonVertex vertex);
        protected abstract void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version);

        protected static float ReadHalf(BinaryReader reader) => new Half { RawValue = reader.ReadUInt16() };
        protected static void WriteHalf(BinaryWriter writer, float value) => writer.Write(new Half(value).RawValue);
        protected static float[] ReadHalfs(BinaryReader reader, int count) => Enumerable.Range(0, count).Select(_ => ReadHalf(reader)).ToArray();

        // A half4 position whose W is the precision scale every other half position uses
        protected static Vector4 ReadScaledPosition(BinaryReader reader)
        {
            var v = ReadHalfs(reader, 4);
            var scale = v[3] != 0 ? v[3] : 1;
            return new Vector4(v[0] * scale, v[1] * scale, v[2] * scale, 1);
        }

        protected static void WriteScaledPosition(BinaryWriter writer, Vector4 position, RmvVersionEnum version)
        {
            var half = VertexLoadHelper.CreatePositionForVersion(position, version, isSkinned: false);
            writer.Write(half.X.RawValue);
            writer.Write(half.Y.RawValue);
            writer.Write(half.Z.RawValue);
            writer.Write(half.W.RawValue);
        }

        // A direction in a half4 whose fourth component is zero in every vanilla vertex
        protected static Vector3 ReadHalfDirection(BinaryReader reader)
        {
            var v = ReadHalfs(reader, 4);
            return new Vector3(v[0], v[1], v[2]);
        }

        protected static void WriteHalfDirection(BinaryWriter writer, Vector3 value)
        {
            WriteHalf(writer, value.X);
            WriteHalf(writer, value.Y);
            WriteHalf(writer, value.Z);
            WriteHalf(writer, 0);
        }

        protected static Vector3 ReadByteDirection(BinaryReader reader) => VertexLoadHelper.CreatVector3_FromByte(new Transforms.ByteVector4 { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte(), W = reader.ReadByte() });
        protected static void WriteByteDirection(BinaryWriter writer, Vector3 value) => writer.Write(VertexLoadHelper.CreateNormalVector3(value));

        protected static float[] Extra(CommonVertex vertex, int count) => vertex.Extra.Length == count ? vertex.Extra : new float[count];
    }

    /// <summary>
    /// Warhammer's wind-swayed props (hanging cloth, bone cages, leaf cards), declared format 12,
    /// stride 20: half3 position + U, half3 normal + V, then a colour whose alpha is the sway
    /// weight. The position is NOT scaled by W, because W is the U coordinate.
    /// </summary>
    public class SwayVertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.Sway;
        public override string Name => "Sway";
        protected override int Stride => 20;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            var position = ReadHalfs(reader, 4);
            var normal = ReadHalfs(reader, 4);
            vertex.Position = new Vector4(position[0], position[1], position[2], 1);
            vertex.Normal = new Vector3(normal[0], normal[1], normal[2]);
            vertex.Uv = new Vector2(position[3], normal[3]);
            vertex.Colour = VertexLoadHelper.CreateColourFromBytes(reader.ReadBytes(4));
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            WriteHalf(writer, vertex.Position.X); WriteHalf(writer, vertex.Position.Y); WriteHalf(writer, vertex.Position.Z); WriteHalf(writer, vertex.Uv.X);
            WriteHalf(writer, vertex.Normal.X); WriteHalf(writer, vertex.Normal.Y); WriteHalf(writer, vertex.Normal.Z); WriteHalf(writer, vertex.Uv.Y);
            var colour = VertexLoadHelper.CreateColourBytes(vertex.Colour);
            writer.Write([colour.X, colour.Y, colour.Z, colour.W]);
        }
    }

    /// <summary>
    /// Trees, shrubs and hedges: declared CustomTerrain (6) but stride 60. Extra: the rest
    /// position (half4, zero on leaf cards) then eight wind halves.
    /// </summary>
    public class VegetationVertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.CustomTerrain;
        public override string Name => "Vegetation";
        protected override int Stride => 60;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            var pivot = ReadHalfs(reader, 4);
            vertex.Position = ReadScaledPosition(reader);
            vertex.Normal = ReadHalfDirection(reader);
            vertex.Tangent = ReadHalfDirection(reader);
            vertex.BiNormal = ReadHalfDirection(reader);
            vertex.Uv = new Vector2(ReadHalf(reader), ReadHalf(reader));
            vertex.Extra = [.. pivot, .. ReadHalfs(reader, 8)];
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            var extra = Extra(vertex, 12);
            foreach (var value in extra.Take(4))
                WriteHalf(writer, value);
            WriteScaledPosition(writer, vertex.Position, version);
            WriteHalfDirection(writer, vertex.Normal);
            WriteHalfDirection(writer, vertex.Tangent);
            WriteHalfDirection(writer, vertex.BiNormal);
            WriteHalf(writer, vertex.Uv.X); WriteHalf(writer, vertex.Uv.Y);
            foreach (var value in extra.Skip(4))
                WriteHalf(writer, value);
        }
    }

    /// <summary>Grass: declared Position16 (5), stride 28, the one layout with float32 uvs.</summary>
    public class GrassVertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.Position16_bit;
        public override string Name => "Grass";
        protected override int Stride => 28;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            vertex.Position = ReadScaledPosition(reader);
            vertex.Uv = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            vertex.Normal = ReadByteDirection(reader);
            vertex.Tangent = ReadByteDirection(reader);
            vertex.BiNormal = ReadByteDirection(reader);
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            WriteScaledPosition(writer, vertex.Position, version);
            writer.Write(vertex.Uv.X); writer.Write(vertex.Uv.Y);
            WriteByteDirection(writer, vertex.Normal);
            WriteByteDirection(writer, vertex.Tangent);
            WriteByteDirection(writer, vertex.BiNormal);
        }
    }

    /// <summary>
    /// A terrain tile's base mesh: declared Position16 (5), stride 8, a half4 and nothing else.
    /// W is NOT a scale here: XYZ alone puts every corner on the tile's grid. Extra: W.
    /// </summary>
    public class PositionHalfVertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.Position16_bit;
        public override string Name => "PositionHalf";
        protected override int Stride => 8;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            var v = ReadHalfs(reader, 4);
            vertex.Position = new Vector4(v[0], v[1], v[2], 1);
            vertex.Extra = [v[3]];
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            WriteHalf(writer, vertex.Position.X); WriteHalf(writer, vertex.Position.Y); WriteHalf(writer, vertex.Position.Z);
            WriteHalf(writer, Extra(vertex, 1)[0]);
        }
    }

    /// <summary>
    /// The flat card a tree collapses to at its furthest LOD: declared 7, stride 28. Half4
    /// position, half4 normal, uv, and four halves that are zero in every vanilla mesh (Extra).
    /// </summary>
    public class TreeBillboardVertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.Rome2Tree;
        public override string Name => "TreeBillboard";
        protected override int Stride => 28;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            vertex.Position = ReadScaledPosition(reader);
            vertex.Normal = ReadHalfDirection(reader);
            vertex.Uv = new Vector2(ReadHalf(reader), ReadHalf(reader));
            vertex.Extra = ReadHalfs(reader, 4);
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            WriteScaledPosition(writer, vertex.Position, version);
            WriteHalfDirection(writer, vertex.Normal);
            WriteHalf(writer, vertex.Uv.X); WriteHalf(writer, vertex.Uv.Y);
            foreach (var value in Extra(vertex, 4))
                WriteHalf(writer, value);
        }
    }

    /// <summary>Half4 position and a uv: declared 7 (Rome 2's trees) or 8 (its water planes), stride 12.</summary>
    public class PositionUvVertexLayout : ExtraVertexLayout
    {
        public PositionUvVertexLayout(VertexFormat declared) => Type = declared;

        public override VertexFormat Type { get; }
        public override string Name => "PositionUv";
        protected override int Stride => 12;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            vertex.Position = ReadScaledPosition(reader);
            vertex.Uv = new Vector2(ReadHalf(reader), ReadHalf(reader));
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            WriteScaledPosition(writer, vertex.Position, version);
            WriteHalf(writer, vertex.Uv.X); WriteHalf(writer, vertex.Uv.Y);
        }
    }
}

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    /// <summary>
    /// The interface banners' vertex (materials 29 and 30, which have no format field), stride 28:
    /// the Static vertex without its second uv. Like Static, the tangent frame is stored X/Z swapped.
    /// </summary>
    public class StaticNoUv2VertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.Unknown;
        public override string Name => "StaticNoUv2";
        protected override int Stride => 28;

        static Vector3 SwapXZ(Vector3 v) => new(v.Z, v.Y, v.X);

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            vertex.Position = ReadScaledPosition(reader);
            vertex.Uv = new Vector2(ReadHalf(reader), ReadHalf(reader));
            vertex.Normal = SwapXZ(ReadByteDirection(reader));
            vertex.Tangent = SwapXZ(ReadByteDirection(reader));
            vertex.BiNormal = SwapXZ(ReadByteDirection(reader));
            vertex.Colour = VertexLoadHelper.CreateColourFromBytes(reader.ReadBytes(4));
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            WriteScaledPosition(writer, vertex.Position, version);
            WriteHalf(writer, vertex.Uv.X); WriteHalf(writer, vertex.Uv.Y);
            WriteByteDirection(writer, SwapXZ(vertex.Normal));
            WriteByteDirection(writer, SwapXZ(vertex.Tangent));
            WriteByteDirection(writer, SwapXZ(vertex.BiNormal));
            var colour = VertexLoadHelper.CreateColourBytes(vertex.Colour);
            writer.Write([colour.X, colour.Y, colour.Z, colour.W]);
        }
    }

    /// <summary>
    /// A bow wave (no material header), stride 24: a half4 position, a second half4 position (where
    /// the crest travels to), a uv and a float of unknown meaning. Extra: the second position, then the float.
    /// </summary>
    public class BowWaveVertexLayout : ExtraVertexLayout
    {
        public override VertexFormat Type => VertexFormat.Unknown;
        public override string Name => "BowWave";
        protected override int Stride => 24;

        protected override void Read(BinaryReader reader, CommonVertex vertex)
        {
            vertex.Position = ReadScaledPosition(reader);
            var secondPosition = ReadHalfs(reader, 4);
            vertex.Uv = new Vector2(ReadHalf(reader), ReadHalf(reader));
            vertex.Extra = [.. secondPosition, reader.ReadSingle()];
        }

        protected override void Write(BinaryWriter writer, CommonVertex vertex, RmvVersionEnum version)
        {
            var extra = Extra(vertex, 5);
            WriteScaledPosition(writer, vertex.Position, version);
            foreach (var value in extra.Take(4))
                WriteHalf(writer, value);
            WriteHalf(writer, vertex.Uv.X); WriteHalf(writer, vertex.Uv.Y);
            writer.Write(extra[4]);
        }
    }
}
