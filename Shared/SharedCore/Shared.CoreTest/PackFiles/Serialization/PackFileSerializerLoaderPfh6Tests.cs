using System.Text;
using Shared.Core.PackFiles.Serialization;
using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Serialization
{
    internal class PackFileSerializerLoaderPfh6Tests
    {
        // As in Pharaoh Dynasties' data\_.pack: 24-byte header, u32 timestamp, 280-byte subheader,
        // then per file: u32 size, u8 is_compressed, zero-terminated path
        internal static byte[] CreatePfh6Pack(params (string Path, byte[] Data)[] files)
        {
            using var index = new MemoryStream();
            using (var indexWriter = new BinaryWriter(index, Encoding.ASCII, leaveOpen: true))
            {
                foreach (var (path, data) in files)
                {
                    indexWriter.Write((uint)data.Length);
                    indexWriter.Write(false);                       // is_compressed
                    indexWriter.Write(Encoding.ASCII.GetBytes(path));
                    indexWriter.Write((byte)0);
                }
            }

            using var pack = new MemoryStream();
            using var writer = new BinaryWriter(pack);
            writer.Write(Encoding.ASCII.GetBytes("PFH6"));
            writer.Write(3);                                // movie pack type, no flags
            writer.Write(0u);                               // pack (dependency) count
            writer.Write(0u);                               // pack index size
            writer.Write((uint)files.Length);
            writer.Write((uint)index.Length);
            writer.Write(0u);                               // timestamp
            var subheader = new byte[280];
            BitConverter.GetBytes(0x12345678u).CopyTo(subheader, 0);
            BitConverter.GetBytes(1u).CopyTo(subheader, 4);
            Encoding.ASCII.GetBytes("CA-TOOL").CopyTo(subheader, 16);
            writer.Write(subheader);
            writer.Write(index.ToArray());
            foreach (var (_, data) in files)
                writer.Write(data);
            writer.Flush();
            return pack.ToArray();
        }

        [Test]
        public void Load_Pfh6_ReadsThePerFileCompressedByte()
        {
            var first = Encoding.ASCII.GetBytes("first file contents");
            var second = Encoding.ASCII.GetBytes("second, longer, file contents");
            var bytes = CreatePfh6Pack((@"terrain\battles\map_info.xml", first), (@"variantmeshes\test\model.rigid_model_v2", second));

            var path = Path.Combine(Path.GetTempPath(), $"pfh6_test_{Guid.NewGuid():N}.pack");
            File.WriteAllBytes(path, bytes);
            try
            {
                using var stream = File.OpenRead(path);
                var pack = PackFileSerializerLoader.Load(path, stream.Length, new BinaryReader(stream), new CustomPackDuplicateFileResolver());

                Assert.That(pack.FileList.Keys, Is.EquivalentTo(new[] { @"terrain\battles\map_info.xml", @"variantmeshes\test\model.rigid_model_v2" }));
                Assert.That(pack.FileList[@"terrain\battles\map_info.xml"].DataSource.ReadData(), Is.EqualTo(first));
                Assert.That(pack.FileList[@"variantmeshes\test\model.rigid_model_v2"].DataSource.ReadData(), Is.EqualTo(second));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
