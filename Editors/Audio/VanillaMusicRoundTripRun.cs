using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Test.Audio
{
    /// <summary>
    /// Every shipped hirc of a type the music build re-emits, written back out and compared with the
    /// bytes it was read from. Music ships as edited copies of vanilla hircs, so a writer that is off
    /// by one field corrupts every .bnk it touches - and a game update changing one of these layouts
    /// shows up here before it shows up in game. Rerun after a game update.
    /// </summary>
    [Explicit("Needs a WH3 install, run manually")]
    internal class VanillaMusicRoundTripRun
    {

        static readonly AkBkHircType[] RewrittenTypes =
            [AkBkHircType.Music_Segment, AkBkHircType.Music_Track, AkBkHircType.Music_Random_Sequence, AkBkHircType.Music_Switch, AkBkHircType.SwitchContainer];

        [SetUp]
        public void RequireInstall() => VanillaBankReader.RequireOnDisk(VanillaBankReader.GameDirectory);

        [Test]
        public void EveryShippedMusicHircWritesBackByteForByte()
        {
            var checkedByType = new Dictionary<AkBkHircType, int>();
            var mismatches = new List<string>();

            var musicBanks = new[] { "audio_base_bnk.pack", "audio_base.pack" }
                .SelectMany(pack => VanillaBankReader.ReadMusicBanks(Path.Combine(VanillaBankReader.GameDirectory, "data", pack)))
                .Where(bank => Path.GetFileName(bank.Path).Contains("music", StringComparison.OrdinalIgnoreCase));

            foreach (var (path, bytes) in musicBanks)
            {
                var rawSections = ReadRawSections(bytes);
                // Media-only .bnks have no HIRC chunk.
                var hircs = (BnkFile.CreateFromBytes(bytes, path, true).HircChunk?.HircItems ?? [])
                    .Where(hirc => !hirc.HasError && hirc is not UnknownHircItem)
                    .Where(hirc => RewrittenTypes.Contains(hirc.HircType) || hirc is CAkAction_V136 { ActionType: AkActionType.SetState });

                foreach (var hirc in hircs)
                {
                    hirc.UpdateSectionSize();
                    if (!hirc.WriteData().AsSpan().SequenceEqual(rawSections[hirc.Id]))
                        mismatches.Add($"{Path.GetFileName(path)}: {hirc.HircType} {hirc.Id}");

                    checkedByType[hirc.HircType] = checkedByType.GetValueOrDefault(hirc.HircType) + 1;
                }
            }

            TestContext.Out.WriteLine(string.Join(", ", checkedByType.Select(entry => $"{entry.Key}: {entry.Value}")));
            Assert.Multiple(() =>
            {
                Assert.That(checkedByType.Keys, Is.SupersetOf(RewrittenTypes), "a type was not found at all - were the music banks moved?");
                Assert.That(mismatches, Is.Empty, $"{mismatches.Count} hirc(s) do not write back as read, first: {string.Join("; ", mismatches.Take(10))}");
            });
        }

        /// <summary>Each hirc's bytes as they are in the .bnk - type, size, id and body - keyed by id.</summary>
        static Dictionary<uint, byte[]> ReadRawSections(byte[] bankBytes)
        {
            var sections = new Dictionary<uint, byte[]>();
            var offset = 0;
            while (offset + 8 <= bankBytes.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(bankBytes, offset, 4);
                var length = (int)BitConverter.ToUInt32(bankBytes, offset + 4);
                if (tag != "HIRC")
                {
                    offset += 8 + length;
                    continue;
                }

                var count = BitConverter.ToUInt32(bankBytes, offset + 8);
                var cursor = offset + 12;
                for (var i = 0; i < count; i++)
                {
                    var total = 5 + (int)BitConverter.ToUInt32(bankBytes, cursor + 1);
                    sections.TryAdd(BitConverter.ToUInt32(bankBytes, cursor + 5), bankBytes[cursor..(cursor + total)]);
                    cursor += total;
                }
                break;
            }
            return sections;
        }
    }
}
