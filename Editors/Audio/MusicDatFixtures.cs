using Shared.GameFormats.MusicDat;

namespace Test.Audio
{
    // The shipped battle_music.dat and campaign_music.dat, kept in Data/Music so the music script
    // tests need no game install.
    internal static class MusicDatFixtures
    {
        public static MusicDatFile Load(string name) => MusicDatParser.Parse(ReadBytes(name));

        public static byte[] ReadBytes(string name)
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, "Data", "Music", name);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            throw new FileNotFoundException($"Data\\Music\\{name} not found above {TestContext.CurrentContext.TestDirectory}");
        }
    }
}
