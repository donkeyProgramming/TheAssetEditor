using System.Text;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace GameWorld.Core.Test.Vfx
{
    // Small vfx files written the way CA's are (UTF-16 with a byte order mark, several root
    // elements), in an in-memory pack under vfx/.
    static class VfxTestFiles
    {
        public static byte[] Xml(string emitters = "", string chains = "", string references = "", string stacks = "", string root = "") =>
        [
            .. Encoding.Unicode.GetPreamble(),
            .. Encoding.Unicode.GetBytes(
                $"""<shader_library file="fx/particle_vfx_library.hlsl"/><vfx revision="2" construction_type="1" enable_in_ted="false"{root}><emitters>{emitters}</emitters><behaviour_chains>{chains}</behaviour_chains><effect_render_stacks>{stacks}</effect_render_stacks></vfx><vfx_references>{references}</vfx_references><!-- EDITOR DATA--><editor_data></editor_data>"""),
        ];

        public static string Emitter(string id, string properties = "") =>
            $"""<emitter type="continuous_mesh" id="{id}"><property name="InfiniteLife" quality="0" value="true"/>{properties}</emitter>""";

        public static string Reference(string target, string posX = "0.00", string posY = "0.00", string rotY = "0.00", string overrides = "", bool infinite = true) =>
            $"""<vfx inst_name="xref" vfx_ref="{target}" notes="" scale_min="1.00" scale_max="1.00" pos_x="{posX}" pos_y="{posY}" pos_z="0.00" rot_x="0.00" rot_y="{rotY}" rot_z="0.00" start_time_min="0.00" start_time_max="0.00" infinite_life="{(infinite ? "true" : "false")}" simulation_speed_multiplier="1.00">{overrides}</vfx>""";

        public static string Override(string name, bool enable = true, string tint = "1.00") =>
            $"""<emitter name="{name}" enable="{(enable ? "true" : "false")}" tint_min_r="{tint}" tint_min_g="{tint}" tint_min_b="{tint}" tint_min_a="1.00" tint_max_r="{tint}" tint_max_g="{tint}" tint_max_b="{tint}" tint_max_a="1.00" particle_lifetime_bias_min="0.00" particle_lifetime_bias_max="0.00" emitter_lifetime_bias_min="0.00" emitter_lifetime_bias_max="0.00" spawn_rate_scale_factor_min="1.00" spawn_rate_scale_factor_max="1.00" sound_record=""/>""";

        /// <summary>A vector field volume laid out as vanilla's: uncompressed A16B16G16R16F, a volume, no mips; x along a row, y
        /// down the rows, z through the slices.</summary>
        public static byte[] Field(int width, int height, int depth, Func<int, int, int, Vector3> at)
        {
            var bytes = new byte[128 + width * height * depth * 8];
            "DDS "u8.CopyTo(bytes);
            foreach (var (offset, value) in new[] { (4, 124), (8, 0x80100F), (12, height), (16, width), (24, depth), (76, 32), (80, 4), (84, 113), (112, 0x200000) })
                BitConverter.TryWriteBytes(bytes.AsSpan(offset), value);   // size, flags, height, width, depth, pixel format, FourCC, D3DFMT_A16B16G16R16F, volume
            var n = 128;
            for (var z = 0; z < depth; z++)
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++, n += 8)
                    {
                        var v = at(x, y, z);
                        BitConverter.TryWriteBytes(bytes.AsSpan(n), (Half)v.X);
                        BitConverter.TryWriteBytes(bytes.AsSpan(n + 2), (Half)v.Y);
                        BitConverter.TryWriteBytes(bytes.AsSpan(n + 4), (Half)v.Z);
                    }
            return bytes;
        }

        /// <summary>Files under vfx/; a name may carry folders below it ("materials/x.xml.material").</summary>
        public static IPackFileService Pack(params (string Name, byte[] Bytes)[] files) => Pack(files.Select(f =>
        {
            var path = "vfx/" + f.Name;
            var slash = path.LastIndexOf('/');
            return new NewPackFileEntry(path[..slash], PackFile.CreateFromBytes(path[(slash + 1)..], f.Bytes));
        }));

        public static IPackFileService Pack(IEnumerable<PackFile> files) => Pack(files.Select(f => new NewPackFileEntry("vfx", f)));

        static IPackFileService Pack(IEnumerable<NewPackFileEntry> entries)
        {
            var packFileService = new PackFileService(null) { EnforceGameFilesMustBeLoaded = false };
            var container = packFileService.CreateNewPackFileContainer("output", PackFileVersion.PFH5, PackFileCAType.MOD, true);
            packFileService.AddFilesToPack(container, entries.ToList());
            return packFileService;
        }
    }
}
