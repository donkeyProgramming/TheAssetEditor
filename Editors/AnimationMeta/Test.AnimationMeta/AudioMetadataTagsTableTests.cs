using System.IO;
using System.Text;
using Editors.Audio.SoundTriggers;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.DB;
using Test.TestingUtility.Shared;

namespace Test.AnimationMeta
{
    public class AudioMetadataTagsTableTests
    {
        const string Tags = "audio_metadata_tags_tables";
        const int Version = 22;

        static object[] Row(string key, string battleEvent = "Derpy_Event", double duration = 0) =>
        [
            .. DbSchema.Versions(Tags)[Version].Select<DbField, object>(x => x.Name switch
            {
                "key" => key,
                "sound_event_battle_start" => battleEvent,
                "persistent_one_shot_duration_override" => duration,
                "vocalisation_bank_split" => "derpy_bank",
                _ => x.Type switch { "Boolean" => false, "I32" => 0, "F32" => 0f, "F64" => 0d, _ => "" },
            })
        ];

        [Test]
        public void ReadsAnF64Column()
        {
            var (version, rows) = DbTableReader.Read(File(Row("derpy_tag", duration: 1.25)), Tags, "test/derpy_f64");

            Assert.That(version, Is.EqualTo(Version));
            Assert.That(rows.Single()["persistent_one_shot_duration_override"], Is.EqualTo(1.25d));
            Assert.That(rows.Single()["vocalisation_bank_split"], Is.EqualTo("derpy_bank"));
        }

        [Test]
        public void ATruncatedFile_IsRefused_NamingTableVersionAndFile()
        {
            var e = Assert.Throws<InvalidDataException>(() => DbTableReader.Read(File(Row("derpy_a"))[..^3], Tags, "test/derpy_cut"));
            Assert.That(e!.Message, Does.Contain(Tags).And.Contain($"v{Version}").And.Contain("test/derpy_cut"));
        }

        [Test]
        public void TrailingBytes_AreRefused()
        {
            var e = Assert.Throws<InvalidDataException>(() => DbTableReader.Read([.. File(Row("derpy_a")), 0], Tags, "test/derpy_long"));
            Assert.That(e!.Message, Does.Contain("test/derpy_long"));
        }

        [Test]
        public void AVersionNotInTheExtract_IsRefused()
        {
            var data = File(Row("derpy_a"));
            data[4] = 99;
            var e = Assert.Throws<InvalidDataException>(() => DbTableReader.Read(data, Tags, "test/derpy_v99"));
            Assert.That(e!.Message, Does.Contain("v99").And.Contain("test/derpy_v99"));
        }

        [Test]
        public void TheCopyTheGameKeeps_ComesFirst()
        {
            var runner = new AssetEditorTestRunner();
            Pack(runner, "zzz_dbtest_z", ("aaa_dbtest_cross", [Row("u2", "z_aaa")]), ("dbtest_samepath", [Row("u4", "z_samepath"), Row("u5", "z_samepath")]));
            Pack(runner, "!!dbtest_a", ("aaa_dbtest_samepack", [Row("u1", "a_aaa")]), ("zzz_dbtest_samepack", [Row("u1", "a_zzz")]),
                ("zzz_dbtest_cross", [Row("u2", "a_zzz")]), ("dbtest_samepath", [Row("u4", "a_samepath")]));

            var rows = DbTables.Read(runner.PackFileService, Tags);
            string[] Copies(string key) => [.. rows.Where(x => x.Text("key") == key).Select(x => x.Text("sound_event_battle_start"))];

            Assert.Multiple(() =>
            {
                Assert.That(Copies("u1"), Is.EqualTo(new[] { "a_aaa", "a_zzz" }), "in one pack, the file whose name sorts first");
                Assert.That(Copies("u2"), Is.EqualTo(new[] { "z_aaa", "a_zzz" }), "the file's name decides, not its pack's");
                Assert.That(Copies("u4"), Is.EqualTo(new[] { "a_samepath" }), "one path in two packs: the first pack keeps its whole file");
                Assert.That(Copies("u5"), Is.Empty, "and the other pack's rows at that path are gone");
            });
            Assert.That(AudioMetadataTags.Read(runner.PackFileService).Find("u1")?.BattleStart, Is.EqualTo("a_aaa"), "the lookup keeps the first copy");
        }

        static void Pack(AssetEditorTestRunner runner, string name, params (string File, object[][] Rows)[] files)
        {
            var pack = runner.CreateEmptyPackFile(name, false);
            runner.PackFileService.AddFilesToPack(pack,
                [.. files.Select(x => new NewPackFileEntry(@"db\" + Tags, PackFile.CreateFromBytes(x.File, File(x.Rows))))]);
        }

        static byte[] File(params object[][] rows)
        {
            var fields = DbSchema.Versions(Tags)[Version];
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(new byte[] { 0xFC, 0xFD, 0xFE, 0xFF });
            writer.Write(Version);
            writer.Write((byte)1);
            writer.Write(rows.Length);
            foreach (var row in rows)
                for (var i = 0; i < fields.Count; i++)
                    Cell(writer, fields[i].Type, row[i]);
            writer.Flush();
            return stream.ToArray();
        }

        static void Cell(BinaryWriter writer, string type, object value)
        {
            switch (type)
            {
                case "OptionalStringU8":
                    var text = (string)value;
                    writer.Write((byte)(text.Length == 0 ? 0 : 1));
                    if (text.Length != 0)
                        Text(writer, text);
                    break;
                case "StringU8": Text(writer, (string)value); break;
                case "Boolean": writer.Write((bool)value); break;
                case "I32": writer.Write((int)value); break;
                case "F32": writer.Write((float)value); break;
                case "F64": writer.Write((double)value); break;
                default: throw new NotSupportedException(type);
            }
        }

        static void Text(BinaryWriter writer, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }
    }
}
