using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.MusicDat;

namespace Editors.Audio.Shared.AudioProject.Compiler
{
    public interface IMusicScriptBuildService
    {
        /// <summary>The scripts with every culture wired in, without saving them, or the problems
        /// that stopped it.</summary>
        MusicScriptBuildResult Build(IReadOnlyList<MusicCulture> musicCultures);

        /// <summary>Builds the scripts and saves them into the editable pack. Returns the problems,
        /// none if they were saved.</summary>
        IReadOnlyList<string> BuildAndSave(IReadOnlyList<MusicCulture> musicCultures);
    }

    public record MusicScriptBuildResult(MusicDatFile Battle, MusicDatFile Campaign, IReadOnlyList<string> Problems);

    /// <summary>
    /// Rebuilds battle_music.dat and campaign_music.dat from the game's own copies with every
    /// culture of a music project wired in, in the order the project lists them. Starting from
    /// vanilla each time, rather than from the copy in the mod pack, is what lets the project be
    /// rebuilt after a game update, and lets a culture be changed or removed by editing the
    /// project.
    /// </summary>
    public class MusicScriptBuildService(IPackFileService packFileService, IFileSaveService fileSaveService) : IMusicScriptBuildService
    {
        public const string BattleScriptPath = @"audio\scripts\battle_music.dat";
        public const string CampaignScriptPath = @"audio\scripts\campaign_music.dat";

        private readonly IPackFileService _packFileService = packFileService;
        private readonly IFileSaveService _fileSaveService = fileSaveService;

        private readonly ILogger _logger = Logging.Create<MusicScriptBuildService>();

        public MusicScriptBuildResult Build(IReadOnlyList<MusicCulture> musicCultures)
        {
            var problems = new List<string>();

            var battle = ReadVanillaScript(BattleScriptPath, problems);
            var campaign = ReadVanillaScript(CampaignScriptPath, problems);
            if (problems.Count != 0)
                return new MusicScriptBuildResult(null, null, problems);

            // Found by shape, not by function name, so a rename in a game update is harmless. A slot
            // the project uses that neither script has any more is not: the culture would silently
            // lose that music, so it is reported.
            var battleSlots = MusicDatCultureWiring.FindSlots(battle).Select(slot => slot.EventPrefix).ToHashSet();
            var campaignSlots = MusicDatCultureWiring.FindSlots(campaign).Select(slot => slot.EventPrefix).ToHashSet();

            foreach (var musicCulture in musicCultures)
            {
                foreach (var missing in musicCulture.Slots.Select(slot => slot.EventPrefix).Where(prefix => !battleSlots.Contains(prefix) && !campaignSlots.Contains(prefix)))
                    problems.Add($"'{musicCulture.AudioState}': neither music script has a {missing} culture chain any more. " +
                                 "The game may have restructured its music scripts; edit this culture to see which slots they offer now");

                // The battle chains match the culture's audio_state and the campaign ones its
                // subculture key; each slot is in exactly one of the two files.
                problems.AddRange(Wire(battle, battleSlots, musicCulture, musicCulture.MusicalCulture));
                problems.AddRange(Wire(campaign, campaignSlots, musicCulture, musicCulture.SubcultureKey));
            }

            return new MusicScriptBuildResult(battle, campaign, problems);
        }

        public IReadOnlyList<string> BuildAndSave(IReadOnlyList<MusicCulture> musicCultures)
        {
            var result = Build(musicCultures);
            if (result.Problems.Count != 0)
            {
                foreach (var problem in result.Problems)
                    _logger.Here().Error($"Music scripts not built: {problem}");
                return result.Problems;
            }

            _logger.Here().Information($"Building the music scripts from vanilla with {musicCultures.Count} culture(s)");
            _fileSaveService.Save(BattleScriptPath, MusicDatParser.Write(result.Battle), false);
            _fileSaveService.Save(CampaignScriptPath, MusicDatParser.Write(result.Campaign), false);
            return [];
        }

        private static IReadOnlyList<string> Wire(MusicDatFile file, HashSet<string> slotPrefixes, MusicCulture musicCulture, string matchKey)
        {
            var plans = musicCulture.Slots
                .Where(slot => slotPrefixes.Contains(slot.EventPrefix))
                .Select(slot => new MusicDatCultureWiring.SlotPlan(slot.EventPrefix, true, slot.ReferenceMatchKey))
                .ToList();
            if (plans.Count == 0)
                return [];

            if (string.IsNullOrWhiteSpace(matchKey))
                return [$"'{musicCulture.AudioState}' has no key to match in {string.Join(", ", plans.Select(plan => plan.EventPrefix))}"];

            return MusicDatCultureWiring.AddCulture(file, matchKey, musicCulture.MusicalCulture, plans)
                .Select(problem => $"'{musicCulture.AudioState}' ({matchKey}): {problem}")
                .ToList();
        }

        /// <summary>The game's own copy, never a mod's - the mod pack holds the last build's, and
        /// building on that would wire every culture in twice. The last game pack holding it wins,
        /// the same as for any other file.</summary>
        private MusicDatFile ReadVanillaScript(string path, List<string> problems)
        {
            var file = _packFileService.GetAllPackfileContainers()
                .Where(container => container.IsCaPackFile)
                .Reverse()
                .Select(container => _packFileService.FindFile(path, container))
                .FirstOrDefault(packFile => packFile != null);

            if (file == null)
            {
                problems.Add($"{path} was not found in the game's packs");
                return null;
            }

            try
            {
                return MusicDatParser.Parse(file);
            }
            catch (Exception e)
            {
                problems.Add($"{path} from the game could not be read, so its format may have changed in a game update: {e.Message}");
                return null;
            }
        }
    }
}
