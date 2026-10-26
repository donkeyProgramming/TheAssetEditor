using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.MusicDat;

namespace Test.Audio
{
    // battle_music.dat and campaign_music.dat as the installed game loads them, so the music script
    // tests always check the current build rather than a copy that goes stale with each game update.
    // Without an install they are skipped - see VanillaBankReader.GameDirectory.
    internal static class MusicDatFixtures
    {
        public const string ScriptFolder = @"audio\scripts";

        static readonly Lazy<Dictionary<string, byte[]>> Scripts = new(ReadFromInstall);

        public static MusicDatFile Load(string name) => MusicDatParser.Parse(ReadBytes(name));

        /// <summary>A copy, so a test can corrupt it.</summary>
        public static byte[] ReadBytes(string name)
        {
            RequireInstall();
            return (byte[])Scripts.Value[name].Clone();
        }

        public static void RequireInstall()
        {
            if (!VanillaBankReader.HasInstall)
                Assert.Ignore($"No WH3 install at {VanillaBankReader.GameDirectory}; set WH3_GAME_DIRECTORY to run the music script tests.");
        }

        // Every pack in the game's manifest, through the same loader and cache the editor uses, so a
        // patch pack overriding a script wins here exactly as it does in the editor and the game.
        static Dictionary<string, byte[]> ReadFromInstall()
        {
            var dataDirectory = Path.Combine(VanillaBankReader.GameDirectory, "data");
            using var provider = VanillaBankReader.CreateProvider(dataDirectory);
            var packFileService = provider.GetRequiredService<IPackFileService>();

            var game = provider.GetRequiredService<IPackFileContainerLoader>().CreateFromGameEnum(PackFileContainerType.Database, GameTypeEnum.Warhammer3)
                ?? throw new InvalidOperationException($"Could not load the game packs from {dataDirectory}");
            packFileService.AddContainer(game);

            return new[] { "battle_music.dat", "campaign_music.dat" }.ToDictionary(name => name, name =>
                packFileService.FindFile($@"{ScriptFolder}\{name}", game)?.DataSource.ReadData()
                    ?? throw new FileNotFoundException($@"{ScriptFolder}\{name} is not in the game's packs any more"));
        }

        /// <summary>A function by name, failing with the likely renames when it is missing. CA renames
        /// script functions between game builds (the October 2026 update renamed three), so this should
        /// say which name to fix rather than "Sequence contains no matching element".</summary>
        public static MusicDatDisassembler.Function RequireFunction(MusicDatFile file, string functionName)
        {
            var functions = MusicDatDisassembler.Disassemble(file);
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function != null)
                return function;

            var similar = functions
                .Select(f => f.Name)
                .Where(name => name.Contains(functionName, StringComparison.OrdinalIgnoreCase) || functionName.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Thrown rather than Assert.Fail, which inside Assert.Multiple only records and carries on.
            throw new InvalidOperationException(
                $"No function '{functionName}' in this script. If the game was just updated it may have been renamed - " +
                (similar.Count != 0 ? $"similar names: {string.Join(", ", similar)}." : "run WriteDumps and look for it."));
        }
    }
}
