using System.Text;
using Shared.GameFormats.RigidModel;

namespace Test.TestingUtility.Shared
{
    public static class RmvTestFiles
    {
        public static readonly ushort[] CubeIndices = Enumerable.Range(0, 6).SelectMany(face => new[] { 0, 1, 2, 2, 3, 0 }.Select(i => (ushort)(face * 4 + i))).ToArray();

        /// <summary>
        /// A single-mesh v8 file laid out like Warhammer 3's rigidmodels/decals/chd/chd_coal_decal_01:
        /// material 100, a 256-byte texture path, 24 declared vertices with no vertex block, and the
        /// 36 indices of a unit cube.
        /// </summary>
        public static byte[] BuildWarhammer3Decal(string texturePath)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(Encoding.ASCII.GetBytes("RMV2"));
            writer.Write(8u);
            writer.Write(1u);
            writer.Write(new byte[128]);

            const uint sectionSize = 80 + 256 + 36 * 2;
            writer.Write(1u);                   // mesh count
            writer.Write(0u);                   // vertex bytes
            writer.Write(36u * 2);              // index bytes
            writer.Write(140u + 28);            // first mesh
            writer.Write(800.0f);               // camera distance
            writer.Write(0u);                   // lod level
            writer.Write(new byte[] { 0, 0x11, 0x22, 0x33 });   // quality level, then stale padding

            writer.Write((ushort)ModelMaterialEnum.alpha_blend);
            writer.Write((ushort)0);
            writer.Write(sectionSize);
            writer.Write(80u + 256);            // vertex offset
            writer.Write(24u);                  // vertex count, with no vertex block
            writer.Write(80u + 256);            // index offset
            writer.Write(36u);
            foreach (var value in new[] { -0.5f, -0.5f, -0.5f, 0.5f, 0.5f, 0.5f })
                writer.Write(value);
            var shader = new byte[12];
            Encoding.ASCII.GetBytes("default_dry").CopyTo(shader, 0);
            writer.Write(shader);
            writer.Write(new byte[20]);

            var path = new byte[256];
            Encoding.ASCII.GetBytes(texturePath).CopyTo(path, 0);
            writer.Write(path);
            foreach (var index in CubeIndices)
                writer.Write(index);

            return stream.ToArray();
        }
    }
}
