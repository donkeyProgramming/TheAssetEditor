using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.Generators
{
    /// <summary>
    /// A vanilla .bnk's bytes with some hircs swapped for edited copies and new ones added, for
    /// shipping under the vanilla .bnk's own name.
    ///
    /// This is the only way found to change a vanilla music hirc. A second .bnk defining the same id
    /// is loaded - every .bnk whose name starts with the context's prefix is, mod files included - but
    /// measured in game (October 2026), its definition never replaces vanilla's whether it loads
    /// before or after vanilla's .bnk, while the same edit spliced into vanilla's own .bnk does.
    ///
    /// Spliced rather than rebuilt: re-serialising the whole file would put every one of its
    /// thousands of hircs through writers that have only ever been asked to write generated ones,
    /// and a single type that does not round trip byte for byte misaligns everything after it. Only
    /// the hircs actually being changed are written, so the rest of vanilla's bytes carry across
    /// untouched. The result embeds the game's current .bnk, so it has to be rebuilt after a game
    /// update changes that .bnk.
    /// </summary>
    public static class VanillaSoundBankSplicer
    {
        public static byte[] Splice(byte[] vanillaBytes, IReadOnlyDictionary<uint, HircItem> replacements, IReadOnlyList<HircItem> additions)
        {
            var (hircChunkStart, hircChunkSize) = FindHircChunk(vanillaBytes);

            var itemCount = BitConverter.ToUInt32(vanillaBytes, hircChunkStart + 8);
            var cursor = hircChunkStart + 12;

            using var body = new MemoryStream();
            var replaced = 0;
            var additionsWritten = false;

            // Vanilla never puts a hirc after something that claims it as a child - across both music
            // .bnks, all 5282 parent-child relations run the other way. So the additions go in ahead of
            // the first hirc being replaced, since a replaced hirc is the only thing that can name one
            // of them, rather than on the end where they would sit after their own parents.
            void WriteAdditions()
            {
                if (additionsWritten)
                    return;

                foreach (var addition in additions)
                {
                    addition.UpdateSectionSize();
                    body.Write(addition.WriteData());
                }

                additionsWritten = true;
            }

            for (uint index = 0; index < itemCount; index++)
            {
                var sectionSize = BitConverter.ToUInt32(vanillaBytes, cursor + 1);
                var id = BitConverter.ToUInt32(vanillaBytes, cursor + 5);
                var totalLength = 5 + (int)sectionSize;

                if (replacements.TryGetValue(id, out var replacement))
                {
                    WriteAdditions();
                    replacement.UpdateSectionSize();
                    body.Write(replacement.WriteData());
                    replaced++;
                }
                else
                    body.Write(vanillaBytes, cursor, totalLength);

                cursor += totalLength;
            }

            if (replaced != replacements.Count)
                throw new InvalidDataException($"Only {replaced} of {replacements.Count} hircs to replace were found in the vanilla .bnk");

            // Nothing in the .bnk is being replaced, so nothing in it can claim these.
            WriteAdditions();

            var bodyBytes = body.ToArray();

            using var output = new MemoryStream();
            output.Write(vanillaBytes, 0, hircChunkStart);
            output.Write(Encoding.ASCII.GetBytes("HIRC"));
            output.Write(BitConverter.GetBytes((uint)(bodyBytes.Length + 4)));
            output.Write(BitConverter.GetBytes(itemCount + (uint)additions.Count));
            output.Write(bodyBytes);

            // Whatever followed the HIRC chunk in the original file.
            var afterHirc = hircChunkStart + 8 + (int)hircChunkSize;
            if (afterHirc < vanillaBytes.Length)
                output.Write(vanillaBytes, afterHirc, vanillaBytes.Length - afterHirc);

            return output.ToArray();
        }

        /// <summary>The ids of every hirc in a .bnk, read from the section headers alone.</summary>
        public static HashSet<uint> ReadHircIds(byte[] bankBytes)
        {
            var (hircChunkStart, _) = FindHircChunk(bankBytes);
            var itemCount = BitConverter.ToUInt32(bankBytes, hircChunkStart + 8);
            var cursor = hircChunkStart + 12;
            var ids = new HashSet<uint>();

            for (uint index = 0; index < itemCount; index++)
            {
                ids.Add(BitConverter.ToUInt32(bankBytes, cursor + 5));
                cursor += 5 + (int)BitConverter.ToUInt32(bankBytes, cursor + 1);
            }

            return ids;
        }

        private static (int Start, uint Size) FindHircChunk(byte[] bankBytes)
        {
            var offset = 0;
            while (offset + 8 <= bankBytes.Length)
            {
                var tag = Encoding.ASCII.GetString(bankBytes, offset, 4);
                var length = BitConverter.ToUInt32(bankBytes, offset + 4);

                if (tag == "HIRC")
                    return (offset, length);

                offset += 8 + (int)length;
            }

            throw new InvalidDataException("The .bnk has no HIRC chunk");
        }
    }
}
