using System.Text;
using Editors.VfxEditor.Document;

namespace Test.VfxEditor
{
    // Effect files laid out the way CA's are: CRLF, tab indents, four root elements, UTF-16 with a BOM.
    static class Fixtures
    {
        public const string CurveString = "x_min,1,1;0.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,0|x_max,2,1;0.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,0:x,0;0,1";

        static string Lines(params string[] lines) => string.Join("\r\n", lines);

        public static readonly string EmitterText = Lines(
            "",
            "\t\t<emitter type=\"continuous_mesh\" id=\"e1\">",
            "\t\t\t<property name=\"A\" quality=\"0\" value=\"1\"/>",
            "\t\t\t<property name=\"A\" quality=\"1\" value=\"1\"/>",
            "\t\t\t<property name=\"A\" quality=\"2\" value=\"5\"/>",
            "\t\t\t<property name=\"B\" quality=\"0\" value=\"7\"/>",
            "\t\t\t<property name=\"C\" quality=\"2\" value=\"3\"/>",
            "\t\t\t<ranged_property name=\"LifeTime\" quality=\"0\" min=\"0.10\" max=\"0.20\"/>",
            "\t\t\t<property name=\"Curve\" quality=\"0\">" + CurveString + "</property>",
            "\t\t</emitter>");

        public static string Emitters(params string[] ids) =>
            string.Concat(ids.Select(id => $"\r\n\t\t<emitter type=\"continuous_mesh\" id=\"{id}\"/>"));

        public static string Block(int tier, string alpha, string colour0, string colour1) => Lines(
            "",
            $"\t\t\t<quality value=\"{tier}\">",
            "\t\t\t\t<modifier fragment=\"ps_texture\">",
            $"\t\t\t\t\t<property name=\"pre_multiplied_alpha\" value=\"{alpha}\"/>",
            "\t\t\t\t\t<texture_property name=\"diffuse\" path=\"vfx/a.dds\"/>",
            "\t\t\t\t</modifier>",
            "\t\t\t\t<modifier fragment=\"vs_colour\">",
            $"\t\t\t\t\t<property name=\"p\" value=\"{colour0}\"/>",
            "\t\t\t\t</modifier>",
            "\t\t\t\t<modifier fragment=\"vs_colour\">",
            $"\t\t\t\t\t<property name=\"p\" value=\"{colour1}\"/>",
            "\t\t\t\t</modifier>",
            "\t\t\t</quality>");

        public static string ChainOf(params string[] blocks) =>
            "\r\n\t\t<behaviour_chain id=\"e1\">" + string.Concat(blocks) + "\r\n\t\t</behaviour_chain>";

        public static readonly string ChainText = ChainOf(Block(0, "true", "1", "1"), Block(1, "true", "1", "1"), Block(2, "false", "1", "1"));

        public static readonly string StackText = Lines(
            "",
            "\t\t<effect_render_stack id=\"e1\">",
            "\t\t\t<property name=\"scene_view_type\" quality=\"0\" value=\"FULLRES_DEFERRED_ALPHA\"/>",
            "\t\t</effect_render_stack>");

        public static string Reference(string target, params string[] rows) =>
            $"\r\n\t<vfx inst_name=\"xref\" vfx_ref=\"{target}\" notes=\"\" scale_min=\"1.00\" scale_max=\"1.00\" pos_x=\"0.00\" pos_y=\"0.00\" pos_z=\"0.00\" rot_x=\"0.00\" rot_y=\"0.00\" rot_z=\"0.00\" start_time_min=\"0.00\" start_time_max=\"0.00\" infinite_life=\"true\" simulation_speed_multiplier=\"1.00\">"
            + string.Concat(rows.Select(r => "\r\n\t\t" + r)) + "\r\n\t</vfx>";

        public static string Row(string name, string enable = "true") => $"<emitter name=\"{name}\" enable=\"{enable}\"/>";

        public static string Effect(string? emitters = null, string? chains = null, string? stacks = null, string? references = null) => Lines(
            "<shader_library file=\"fx/particle_vfx_library.hlsl\"/><vfx revision=\"2\" construction_type=\"1\" enable_in_ted=\"false\">",
            "\t<!-- EMITTERS-->",
            "\t<emitters>" + (emitters ?? EmitterText),
            "\t</emitters>",
            "\t<behaviour_chains>" + (chains ?? ChainText),
            "\t</behaviour_chains>",
            "\t<effect_render_stacks>" + (stacks ?? StackText),
            "\t</effect_render_stacks>",
            "</vfx>",
            "<vfx_references>" + (references ?? Reference("lib", Row("a"), Row("b", "false"))),
            "</vfx_references>",
            "<!-- EDITOR DATA-->",
            "<editor_data>",
            "</editor_data>");

        public static readonly string Default = Effect();

        /// <summary>Default with the editor_data CA's editor writes, the way vanilla has it: a component per emitter and per reference.</summary>
        public static readonly string WithComponents = Default.Replace("<editor_data>\r\n</editor_data>",
            "<editor_data>\r\n\t<effect_components>\r\n\t\t<component id=\"e1\"/>\r\n\t\t<component id=\"xref\"/>\r\n\t</effect_components>\r\n</editor_data>");

        public static IEnumerable<string?> Components(VfxDocument document) =>
            document.Root.Child("editor_data")!.Child("effect_components")!.Children.Select(c => c.Attr("id"));

        public static byte[] Utf16(string text) => [0xFF, 0xFE, .. Encoding.Unicode.GetBytes(text)];

        /// <summary>An uncompressed 32-bit DDS of one colour, which Pfim reads as Rgba32.</summary>
        public static byte[] Dds(int width, int height, byte r, byte g, byte b, byte a)
        {
            var header = new int[31];
            (header[0], header[1], header[2], header[3], header[4]) = (124, 0x100F, height, width, width * 4);   // size, flags, height, width, pitch
            (header[18], header[19], header[21]) = (32, 0x41, 32);   // pixel format: size, RGB with alpha, 32 bits
            (header[22], header[23], header[24], header[25]) = (0x00FF0000, 0x0000FF00, 0x000000FF, unchecked((int)0xFF000000));
            header[26] = 0x1000;   // a texture
            var bytes = new List<byte>(Encoding.ASCII.GetBytes("DDS "));
            bytes.AddRange(header.SelectMany(BitConverter.GetBytes));
            for (var i = 0; i < width * height; i++)
                bytes.AddRange([b, g, r, a]);
            return [.. bytes];
        }

        public static VfxDocument Document(string? text = null) => VfxDocument.Load(Utf16(text ?? Default));

        public static SpanElement Emitter(VfxDocument document) => document.Section("emitters")!.Children[0];
        public static SpanElement Chain(VfxDocument document) => document.Section("behaviour_chains")!.Children[0];
        public static SpanElement Stack(VfxDocument document) => document.Section("effect_render_stacks")!.Children[0];
    }
}
