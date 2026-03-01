using System;

namespace AssetEditor.Services.Ipc
{
    public static class PackPathResolver
    {
        private static readonly string[] KnownRoots =
        [
            "variantmeshes\\",
            "ui\\",
            "animations\\",
            "audio\\"
        ];

        public static string ResolvePackPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var path = input.Trim();

            if (path.Length >= 2)
            {
                var first = path[0];
                var last = path[^1];
                var hasMatchingQuotes = (first == '"' && last == '"') || (first == '\'' && last == '\'');
                if (hasMatchingQuotes)
                    path = path[1..^1];
            }

            path = path.Replace('/', '\\');
            while (path.Contains("\\\\", StringComparison.Ordinal))
                path = path.Replace("\\\\", "\\", StringComparison.Ordinal);
            var lowerPath = path.ToLowerInvariant();

            foreach (var knownRoot in KnownRoots)
            {
                var index = lowerPath.IndexOf(knownRoot, StringComparison.Ordinal);
                if (index >= 0)
                    return path[index..];
            }

            return path;
        }
    }
}
