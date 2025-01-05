using System.Text;

namespace Shared.Core.PackFiles
{
    public static class PackFileEncrypter
    {
        private static readonly byte[] s_iNDEX_STRING_KEY = Encoding.ASCII.GetBytes("#:AhppdV-!PEfz&}[]Nv?6w4guU%dF5.fq:n*-qGuhBJJBm&?2tPy!geW/+k#pG?");
        private const uint INDEX_U32_KEY = 0xE10B_73F4;
        private const ulong DATA_KEY = 0x8FEB_2A67_40A6_920E;

        public static byte[] Encrypt(byte[] plaintext)
        {
            // Ensure the plaintext is a multiple of 8 bytes by padding with zeros if necessary.
            var size = plaintext.Length;
            var padding = 8 - size % 8;
            if (padding < 8)
                Array.Resize(ref plaintext, size + padding);

            var ciphertext = new byte[plaintext.Length];
            ulong edi = 0;
            var chunks = plaintext.Length / 8;

            using (var memStream = new MemoryStream(plaintext))
            using (var reader = new BinaryReader(memStream))
            using (var writer = new BinaryWriter(new MemoryStream(ciphertext)))
            {
                for (var i = 0; i < chunks; i++)
                {
                    if (i == chunks - 1)
                        writer.Write(reader.ReadBytes(8));  // Do not encrypt the last chunk.
                    else
                    {
                        var esi = edi;
                        memStream.Seek((long)esi, SeekOrigin.Begin);
                        var data = reader.ReadUInt64();
                        var encrypted = data ^ (DATA_KEY * ~edi);
                        writer.Seek((int)esi, SeekOrigin.Begin);
                        writer.Write(encrypted);
                    }
                    edi += 8;
                }
            }

            // Remove extra padding for accurate file representation.
            Array.Resize(ref ciphertext, size);
            return ciphertext;
        }

        // This function encrypts a uint32 value (like the PackedFile size).
        public static uint EncryptU32(uint plaintext, uint secondKey)
        {
            return plaintext ^ INDEX_U32_KEY ^ ~secondKey;
        }

        // This function encrypts a file path into the pack file.
        public static byte[] EncryptString(string path, uint secondKey)
        {
            var pathBytes = Encoding.ASCII.GetBytes(path);
            var encrypted = new byte[pathBytes.Length + 1];  // +1 for null terminator
            var index = 0;

            for (var i = 0; i < pathBytes.Length; i++)
            {
                encrypted[i] = (byte)(pathBytes[i] ^ s_iNDEX_STRING_KEY[index % s_iNDEX_STRING_KEY.Length] ^ ~secondKey);
                index++;
            }

            encrypted[pathBytes.Length] = 0;  // Null terminator
            return encrypted;
        }
    }
}
