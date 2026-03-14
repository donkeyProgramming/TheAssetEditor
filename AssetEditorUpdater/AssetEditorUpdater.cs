using System.Diagnostics;
using Octokit;
using SharpCompress.Archives;
using FileMode = System.IO.FileMode;

namespace AssetEditorUpdater
{
    public class AssetEditorUpdater
    {
        private const string GitHubOwner = "donkeyProgramming";
        private const string GitHubRepository = "TheAssetEditor";
        private const string AssetEditorExe = "AssetEditor.exe";
        private const string AssetEditorUpdaterExe = "AssetEditorUpdater.exe";
        // We assume the release RAR contains a single folder named AssetEditor with all the files for the update in it
        private const string UpdateFilesDirectoryName = "AssetEditor";
        private const string InstallationUpdateBackupDirectoryName = "UpdateBackup";
         
        public static async Task Main(string[] args)
        {
            // The first time the updater is run it should be from the installation directory (with no args)
            // so we use the app's base directory to get the installation directory. When we rerun the updater
            // from the update directory we pass the installation directory as an arg and access it that way.

            var currentDirectory = AppContext.BaseDirectory;
            var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var updateDirectory = Path.Combine(userDirectory, "AssetEditor", "Temp", "Update");

            var isInitialLaunch = args.Length == 0;
            var installationDirectory = isInitialLaunch ? currentDirectory : args[0];

            Console.WriteLine($"Running updater from {currentDirectory}.");

            if (isInitialLaunch)
                RelaunchFromUpdateDirectory(updateDirectory, installationDirectory);
            else
                await UpdateAsync(installationDirectory, updateDirectory);
        }

        private static void RelaunchFromUpdateDirectory(string updateDirectory, string installationDirectory)
        {
            Console.WriteLine($"Copying updater to {updateDirectory} and relaunching...");

            if (!Directory.Exists(updateDirectory))
                Directory.CreateDirectory(updateDirectory);

            var currentUpdaterPath = Path.Combine(installationDirectory, AssetEditorUpdaterExe);
            var newUpdaterPath = Path.Combine(updateDirectory, AssetEditorUpdaterExe);
            File.Copy(currentUpdaterPath, newUpdaterPath, true);

            LaunchUpdater(newUpdaterPath, updateDirectory, installationDirectory);
        }

        private static void LaunchUpdater(string updaterPath, string workingDirectory, string installationDirectory)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                ArgumentList = { installationDirectory }
            };
            Process.Start(processStartInfo);
        }

        private static async Task UpdateAsync(string installationDirectory, string updateDirectory)
        {
            var latestRelease = await GetLatestReleaseAsync();
            if (latestRelease == null)
                return;

            var installedVersion = GetAssetEditorVersion(installationDirectory);
            var latestVersion = ParseReleaseVersion(latestRelease.TagName);
            if (installedVersion >= latestVersion)
            {
                Console.WriteLine("No update required.");
                return;
            }

            Console.WriteLine($"Updating AssetEditor from version {installedVersion} to {latestVersion}.");

            var asset = GetAsset(latestRelease);
            var assetPath = Path.Combine(updateDirectory, asset.Name);
            var downloadResult = await DownloadAssetAsync(asset.BrowserDownloadUrl, assetPath);
            if (downloadResult == false)
                return;

            BackupOldFiles(installationDirectory);

            ExtractRar(assetPath, installationDirectory);

            var assetEditorPath = Path.Combine(installationDirectory, AssetEditorExe);
            if (File.Exists(assetEditorPath))
                LaunchAssetEditor(installationDirectory, assetEditorPath);
            else
                Console.WriteLine("Uh oh something with the update went wrong.");

            Console.WriteLine("Press any key to close.");
            Console.ReadKey();
        }

        private static async Task<Release?> GetLatestReleaseAsync()
        {
            try
            {
                var gitHubClient = new GitHubClient(new ProductHeaderValue("AssetEditor"));
                var releases = await gitHubClient.Repository.Release.GetAll(GitHubOwner, GitHubRepository);
                return releases.Count > 0 ? releases[0] : null;
            }
            catch (ApiException exception)
            {
                Console.WriteLine($"Unable to retrieve latest release from GitHub: {exception.Message}");
                return null;
            }
        }

        private static Version GetAssetEditorVersion(string installationDirectory)
        {
            var assetEditorPath = Path.Combine(installationDirectory, AssetEditorExe);
            var versionInfo = FileVersionInfo.GetVersionInfo(assetEditorPath);
            if (string.IsNullOrWhiteSpace(versionInfo.FileVersion))
                return new Version();

            var parsedVersion = new Version(versionInfo.FileVersion);
            if (parsedVersion.Build == 0 && parsedVersion.Revision == 0)
                return new Version(parsedVersion.Major, parsedVersion.Minor);

            if (parsedVersion.Revision == 0)
                return new Version(parsedVersion.Major, parsedVersion.Minor, parsedVersion.Build);

            return parsedVersion;
        }

        private static Version ParseReleaseVersion(string tagName)
        {
            var cleanedVersion = tagName.Trim().TrimStart('v', 'V');
            return new Version(cleanedVersion);
        }

        private static ReleaseAsset GetAsset(Release latestRelease)
        {
            if (latestRelease.Assets.Count != 1)
                throw new InvalidOperationException($"Expected 1 asset, found {latestRelease.Assets.Count}.");

            var asset = latestRelease.Assets[0];
            var extension = Path.GetExtension(asset.Name);
            if (extension != ".rar")
                throw new InvalidOperationException($"Asset has extension {extension}, expected .rar.");

            return asset;
        }

        private static async Task<bool> DownloadAssetAsync(string downloadUrl, string downloadPath)
        {
            Console.WriteLine("Downloading the latest release...");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AssetEditor_instance");

                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await responseStream.CopyToAsync(fileStream);
                
                return true;
            }
            catch
            {
                Console.WriteLine("Unable to download latest release from Github.");
                return false;
            }
        }

        private static void BackupOldFiles(string installationDirectory)
        {
            var updateBackupDirectory = Path.Combine(installationDirectory, InstallationUpdateBackupDirectoryName);
            if (Directory.Exists(updateBackupDirectory))
                Directory.Delete(updateBackupDirectory, true);
            Directory.CreateDirectory(updateBackupDirectory);

            Console.WriteLine($"Backing up files from {installationDirectory} to {updateBackupDirectory}...");

            foreach (var entryPath in Directory.EnumerateFileSystemEntries(installationDirectory))
            {
                var entryName = Path.GetFileName(entryPath);
                if (entryName == InstallationUpdateBackupDirectoryName)
                    continue;

                var destinationPath = Path.Combine(updateBackupDirectory, entryName);
                if (Directory.Exists(entryPath))
                    Directory.Move(entryPath, destinationPath);
                else
                    File.Move(entryPath, destinationPath);
            }
        }

        private static void ExtractRar(string rarPath, string installationDirectory)
        {
            Console.WriteLine($"Extracting update files from {rarPath} to {installationDirectory}...");

            var prefix = UpdateFilesDirectoryName + Path.DirectorySeparatorChar;

            using var archive = ArchiveFactory.OpenArchive(rarPath);
            foreach (var entry in archive.Entries)
            {
                if (entry.Key == null || entry.Key == UpdateFilesDirectoryName || entry.IsDirectory)
                    continue;

                var entryKeyWithoutPrefix = entry.Key[prefix.Length..];
                var destinationPath = Path.Combine(installationDirectory, entryKeyWithoutPrefix);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.WriteToFile(destinationPath);
            }
        }

        private static void LaunchAssetEditor(string installationDirectory, string assetEditorPath)
        {
            Console.WriteLine("Update complete.");
            Console.WriteLine("Relaunching AssetEditor...");

            // We launch using explorer.exe as that ensures admin priveliges aren't used as they
            // would otherwise automatically be used if the AssetEditorUpdater.exe required them.
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                WorkingDirectory = installationDirectory,
                UseShellExecute = true,
                Arguments = $"\"{assetEditorPath}\"".Trim()
            };
            Process.Start(processStartInfo);
        }
    }
}
