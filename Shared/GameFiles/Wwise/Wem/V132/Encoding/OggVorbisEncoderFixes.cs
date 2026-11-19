using System.Reflection;
using OggVorbisEncoder.Setup;

namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    /// <summary>
    /// Runtime fixes for OggVorbisEncoder 1.2.2 (the latest release), whose setup tables differ from
    /// libvorbis's (lib/modes/residue_*.h). Checked against ffmpeg's libvorbis, decoding with NVorbis and ffmpeg:
    /// 1. Wrong mono residue tables: 44.1/48kHz mono at quality 0.2-0.4 (0.4 is the Audio Editor's default) decoded as noise.
    /// 2. Residue44High_Coupled holds the low residue, quietening stereo from quality 0.6; stereo 16/22kHz's
    ///    upper-quality residue is type 1 instead of 2.
    /// 3. ResidueEntry.Clone shares its arrays with the static template, so one encode could corrupt the next.
    /// 4. Every encode dropped its first half long block (see CreateProcessingState).
    /// A table fix is only applied while the library still holds the known-wrong entry.
    /// </summary>
    internal static class OggVorbisEncoderFixes
    {
        private static readonly ILogger s_logger = Logging.CreateStatic(typeof(OggVorbisEncoderFixes));
        private static readonly Lock s_lock = new();
        private static bool s_applied;

        private const string ResidueNamespace = "OggVorbisEncoder.Setup.Templates.Residue";

        public static void EnsureApplied()
        {
            lock (s_lock)
            {
                if (s_applied)
                    return;
                s_applied = true;

                try
                {
                    Apply();
                }
                catch (Exception e)
                {
                    // Never block encoding on the patch itself; the output is just no better than before.
                    s_logger.Here().Error($"Could not apply OggVorbisEncoder fixes, Vorbis encoding may produce broken audio: {e}");
                }
            }
        }

        private static readonly FieldInfo? s_pcmCurrentField = typeof(OggVorbisEncoder.ProcessingState).GetField("_pcmCurrent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? s_centerWindowField = typeof(OggVorbisEncoder.ProcessingState).GetField("_centerWindow", BindingFlags.Instance | BindingFlags.NonPublic);

        // libvorbis starts writing input at centerW (_vds_shared_init); the port starts at 0, so the first
        // half block landed in the pre-roll that PreExtrapolate overwrites
        public static OggVorbisEncoder.ProcessingState CreateProcessingState(OggVorbisEncoder.VorbisInfo info)
        {
            var state = OggVorbisEncoder.ProcessingState.Create(info);
            if (s_pcmCurrentField == null || s_centerWindowField == null)
            {
                s_logger.Here().Warning("OggVorbisEncoder.ProcessingState layout changed, cannot fix the dropped start of each encode");
                return state;
            }

            if ((int)s_pcmCurrentField.GetValue(state)! == 0)
                s_pcmCurrentField.SetValue(state, s_centerWindowField.GetValue(state));
            return state;
        }

        private static void Apply()
        {
            var assembly = typeof(ResidueEntry).Assembly;
            var shared = GetType(assembly, "Shared");
            var lowUncoupled = GetStatic<ResidueEntry>(shared, "Residue44Low_Uncoupled");
            var mediumUncoupled = GetStatic<ResidueEntry>(shared, "Residue44Medium_Uncoupled");
            var lowCoupled = GetStatic<ResidueEntry>(shared, "Residue44Low_Coupled");
            var mediumCoupled = GetStatic<ResidueEntry>(shared, "Residue44Medium_Coupled");
            var highCoupled = GetStatic<ResidueEntry>(shared, "Residue44High_Coupled");

            // libvorbis _residue_44_high
            var correctHighCoupled = new ResidueEntry(0, -1, -1, 10, -1, -1, [0], [-1],
                [0, 1, 2, 4, 8, 16, 32, 71, 157],
                [0, 1, 2, 3, 4, 8, 16, 71, 157],
                ResidueType.Zero);

            var uncoupled44 = GetStatic<IMappingTemplate[]>(GetType(assembly, "Residue44"), "MapRes_Uncoupled");
            var uncoupled16 = GetStatic<IMappingTemplate[]>(GetType(assembly, "Residue16"), "MapRes_Uncoupled");
            var uncoupled8 = GetStatic<IMappingTemplate[]>(GetType(assembly, "Residue8"), "MapRes_Uncoupled");

            // MapRes_Uncoupled[i] for 44kHz is libvorbis _res_44u_(i-1), index 0 being _res_44u_n1.
            ReplaceResidue(uncoupled44, 3, mediumUncoupled, lowUncoupled);     // _res_44u_2
            ReplaceResidue(uncoupled44, 4, mediumUncoupled, lowUncoupled);     // _res_44u_3
            ReplaceResidue(uncoupled44, 5, mediumUncoupled, lowUncoupled);     // _res_44u_4
            ReplaceResidue(uncoupled44, 7, GetStatic<ResidueEntry>(shared, "Residue44High_Uncoupled"), mediumUncoupled); // _res_44u_6
            ReplaceResidue(uncoupled44, 8, GetStatic<ResidueEntry>(shared, "Residue44High_Uncoupled"), mediumUncoupled); // _res_44u_7
            ReplaceResidue(uncoupled16, 0, lowCoupled, lowUncoupled);          // _res_16u_0
            ReplaceResidue(uncoupled16, 1, mediumCoupled, mediumUncoupled);    // _res_16u_1
            ReplaceResidue(uncoupled8, 0, mediumUncoupled, lowUncoupled);      // _res_8u_0

            // _res_16s_2 (stereo 16/22kHz, upper qualities) is residue type 2 in libvorbis, type 1 in the port.
            var stereo16 = GetStatic<IMappingTemplate[]>(GetType(assembly, "Residue16"), "MapRes_Stereo");
            var stereo16High = stereo16[2].ResidueTemplate;
            for (var r = 0; r < stereo16High.Length; r++)
            {
                var template = stereo16High[r];
                if (template.ResidueType == ResidueType.One && ReferenceEquals(template.Residue, highCoupled))
                    stereo16High[r] = new ResidueTemplate(ResidueType.Two, template.LimitType, template.Grouping, template.Residue,
                        template.BookAux, template.BookAuxManaged, template.BooksBase, template.BooksBaseManaged);
            }

            var allMaps = new[] { "Residue44", "Residue16", "Residue8" }
                .SelectMany(typeName => GetType(assembly, typeName)
                    .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(f => f.FieldType == typeof(IMappingTemplate[]))
                    .Select(f => (IMappingTemplate[])f.GetValue(null)!))
                .ToList();

            foreach (var maps in allMaps)
            {
                for (var i = 0; i < maps.Length; i++)
                {
                    var residueTemplates = maps[i].ResidueTemplate;
                    for (var r = 0; r < residueTemplates.Length; r++)
                    {
                        var template = residueTemplates[r];
                        var entry = ReferenceEquals(template.Residue, highCoupled) ? correctHighCoupled : template.Residue;

                        // Every template gets its own entry with fresh book arrays (see 3. above)
                        residueTemplates[r] = new ResidueTemplate(template.ResidueType, template.LimitType, template.Grouping, CopyEntry(entry),
                            template.BookAux, template.BookAuxManaged, template.BooksBase, template.BooksBaseManaged);
                    }
                }
            }
        }

        private static void ReplaceResidue(IMappingTemplate[] maps, int mapIndex, ResidueEntry expectedWrong, ResidueEntry correct)
        {
            var residueTemplates = maps[mapIndex].ResidueTemplate;
            for (var r = 0; r < residueTemplates.Length; r++)
            {
                var template = residueTemplates[r];
                if (!ReferenceEquals(template.Residue, expectedWrong))
                    continue;

                residueTemplates[r] = new ResidueTemplate(template.ResidueType, template.LimitType, template.Grouping, correct,
                    template.BookAux, template.BookAuxManaged, template.BooksBase, template.BooksBaseManaged);
            }
        }

        // The templates are defined with empty second stages and books { -1 }, which FillBooks fills in during setup
        private static ResidueEntry CopyEntry(ResidueEntry entry) => new(entry.Begin, entry.End, entry.Grouping, entry.Partitions, entry.PartitionValues, entry.GroupBook,
            [0], [-1], entry.ClassMetric1.ToArray(), entry.ClassMetric2.ToArray(), entry.ResidueType);

        private static Type GetType(Assembly assembly, string name) =>
            assembly.GetType($"{ResidueNamespace}.{name}") ?? throw new InvalidOperationException($"OggVorbisEncoder type {ResidueNamespace}.{name} not found");

        private static T GetStatic<T>(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException($"OggVorbisEncoder field {type.Name}.{name} not found");
            return (T)field.GetValue(null)!;
        }
    }
}
