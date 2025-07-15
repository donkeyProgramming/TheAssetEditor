using System.Buffers.Binary;
using System.Text;

namespace Shared.Core.PackFiles
{
    public static class PackFileEncryption
    {
        private static readonly byte[] s_iNDEX_STRING_KEY = Encoding.ASCII.GetBytes("#:AhppdV-!PEfz&}[]Nv?6w4guU%dF5.fq:n*-qGuhBJJBm&?2tPy!geW/+k#pG?");
        private const uint INDEX_U32_KEY = 0xE10B_73F4;
        private const ulong DATA_KEY = 0x8FEB_2A67_40A6_920E;

        public static byte[] Decrypt(byte[] ciphertext)
        {
            // First, make sure the file ends in a multiple of 8. If not, extend it with zeros.
            // We need it because the decoding is done in packs of 8 bytes.
            var size = ciphertext.Length;
            var padding = 8 - size % 8;
            if (padding < 8)
                Array.Resize(ref ciphertext, size + padding);

            // Then decrypt the file in packs of 8. It's faster than in packs of 4.
            var plaintext = new byte[ciphertext.Length];
            ulong edi = 0;
            var chunks = ciphertext.Length / 8;

            using (var memStream = new MemoryStream(ciphertext))
            using (var reader = new BinaryReader(memStream))
            using (var writer = new BinaryWriter(new MemoryStream(plaintext)))
            {
                for (var i = 0; i < chunks; i++)
                {
                    if (i == chunks - 1)
                        writer.Write(reader.ReadBytes(8));  // The last chunk is not encrypted.
                    else
                    {
                        var esi = edi;
                        memStream.Seek((long)esi, SeekOrigin.Begin);
                        var prod = DATA_KEY * ~edi;
                        var data = reader.ReadUInt64();
                        prod ^= data;
                        writer.Seek((int)esi, SeekOrigin.Begin);
                        writer.Write(prod);
                    }
                    edi += 8;
                }
            }

            // Remove the extra bytes we added in the first step.
            Array.Resize(ref plaintext, size);
            return plaintext;
        }

        public static void DecryptInPlace(Span<byte> buffer, long entrySize, long entryRelativeOffset = 0)
        {
            // We need it because the decoding is done in packs of 8 bytes.
            if (entrySize <= 8)
                return;

            for (var off = 0; off + 8 <= buffer.Length; off += 8)
            {
                var edi = entryRelativeOffset + off;
                if (edi + 8 > entrySize - 8)
                    break;

                var cipher = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(off, 8));
                var plain = DATA_KEY * ~((ulong)edi) ^ cipher;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(off, 8), plain);
            }
        }

        // This function decrypts the size of a PackedFile.
        public static uint DecryptAndReadU32(BinaryReader reader, uint secondKey)
        {
            var ciphertext = reader.ReadUInt32();
            return ciphertext ^ INDEX_U32_KEY ^ ~secondKey;
        }

        // This function decrypts the path of a PackedFile.
        public static string DecryptAndReadString(Stream stream, uint secondKey)
        {
            StringBuilder path = new();
            var index = 0;
            while (true)
            {
                var character = stream.ReadByte();
                if (character == -1) break;

                var decryptedChar = (byte)(character ^ s_iNDEX_STRING_KEY[index % s_iNDEX_STRING_KEY.Length] ^ ~secondKey);
                if (decryptedChar == 0) break;

                path.Append((char)decryptedChar);
                index++;
            }
            return path.ToString();
        }

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
