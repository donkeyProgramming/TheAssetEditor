namespace Shared.Core.PackFiles.Utility
{
    static class ManifestHelper
    {
        public static List<string> GetPackFilesFromManifest(string gameDataFolder, out bool manifestFileFound)
        {
            var output = new List<string>();
            var manifestFile = gameDataFolder + "\\manifest.txt";
            if (File.Exists(manifestFile))
            {
                var lines = File.ReadAllLines(manifestFile);
                foreach (var line in lines)
                {
                    var items = line.Split('\t');
                    if (items[0].Contains(".pack"))
                        output.Add(Path.Combine(gameDataFolder, items[0].Trim()));
                }
                manifestFileFound = true;
                return output;
            }
            else
            {
                var files = Directory.GetFiles(gameDataFolder)
                    .Where(x => Path.GetExtension(x).ToLower() == ".pack")
                    .ToList();

                manifestFileFound = false;
                return files;
            }
        }
    }
}
