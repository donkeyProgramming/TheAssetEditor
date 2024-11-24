namespace Shared.Core.PackFiles
{
    static class ManifestHelper
    {
        public static List<string> GetPackFilesFromManifest(string gameDataFolder)
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
                        output.Add(items[0].Trim());
                }
                return output;
            }
            else
            {
                var files = Directory.GetFiles(gameDataFolder)
                    .Where(x => Path.GetExtension(x) == ".pack")
                    .Select(x => Path.GetFileName(x))
                    .ToList();
                return files;
            }
        }
    }
}
