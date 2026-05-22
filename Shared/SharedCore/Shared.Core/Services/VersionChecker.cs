using System.Reflection;
using Octokit;
using Shared.Core.ErrorHandling;

namespace Shared.Core.Services
{
    public class VersionChecker
    {
        private static readonly ILogger s_logger = Logging.Create<VersionChecker>();

        private const string GitHubOwner = "donkeyProgramming";
        private const string GitHubRepository = "TheAssetEditor";

        public static async Task<List<Release>?> GetNewerReleases()
        {
            var releases = await GetReleasesAsync();
            if (releases == null || releases.Count == 0)
                return null;

            var currentVersion = GetCurrentVersion();
            //currentVersion = new Version("0.65");
            var newerReleases = GetReleasesSinceCurrentVersion(releases, currentVersion);
            if (newerReleases.Count > 0)
                return newerReleases;

            return null;
        }

        private static async Task<IReadOnlyList<Release>?> GetReleasesAsync()
        {
            try
            {
                var gitHubClient = new GitHubClient(new ProductHeaderValue("AssetEditor"));
                var releases = await gitHubClient.Repository.Release.GetAll(GitHubOwner, GitHubRepository);
                return releases.Count > 0 ? releases : null;
            }
            catch (ApiException exception)
            {
                s_logger.Information($"Unable to retrieve latest release from GitHub: {exception.Message}");
                return null;
            }
        }

        private static List<Release> GetReleasesSinceCurrentVersion(IReadOnlyList<Release> releases, Version currentVersion)
        {
            var newerReleases = new List<Release>();
            foreach (var release in releases)
            {
                var releaseVersion = ParseReleaseVersion(release.TagName);
                if (releaseVersion > currentVersion)
                    newerReleases.Add(release);
                else
                    break;
            }
            return newerReleases;
        }

        public static Version GetCurrentVersion()
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            if (version == null)
                throw new Exception("Current version is unknown.");

            if (version.Build == 0 && version.Revision == 0)
                return new Version(version.Major, version.Minor);

            return new Version(version.Major, version.Minor, version.Build);
        }

        public static Version ParseReleaseVersion(string tagName)
        {
            var cleanedVersion = tagName.Trim().TrimStart('v', 'V');
            return new Version(cleanedVersion);
        }
    }
}
