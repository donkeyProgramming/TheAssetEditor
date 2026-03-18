using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.Services;
using Application = System.Windows.Application;

namespace AssetEditor.ViewModels
{
    public class ReleaseNoteItem(Release release)
    {
        public string ReleaseName { get; } = $"## [{release.Name}]({release.HtmlUrl})";
        public string PublishedAt { get; } = $"{release.PublishedAt!.Value:dd MMM yyyy}";
        public string ReleaseNotes { get; } = release.Body;
    }

    public partial class UpdaterViewModel(LocalizationManager localisationManager) : ObservableObject
    {
        private readonly LocalizationManager _localisationManager = localisationManager;

        private readonly ILogger _logger = Logging.Create<UpdaterViewModel>();
        private Action? _closeAction;

        private const string AssetEditorUpdaterExe = "AssetEditorUpdater.exe";

        private List<Release> _newerReleases = [];

        [ObservableProperty] private ObservableCollection<ReleaseNoteItem> _releaseNotesItems = [];
        [ObservableProperty] private string _updateInfo = string.Empty;

        public void SetReleaseInfo(List<Release> newerReleases)
        {
            _newerReleases = newerReleases;

            var latestRelease = _newerReleases[0];
            var latestVersion = VersionChecker.ParseReleaseVersion(latestRelease.TagName);
            var currentVersion = VersionChecker.GetCurrentVersion();
            UpdateInfo = string.Format(_localisationManager.Get("UpdaterWindow.UpdateInfo"), currentVersion, latestVersion);

            ReleaseNotesItems.Clear();
            foreach (var release in _newerReleases)
                ReleaseNotesItems.Add(new ReleaseNoteItem(release));
        }

        [RelayCommand] public void Update()
        {
            if (Debugger.IsAttached)
                return;

            var currentVersion = VersionChecker.GetCurrentVersion();
            var latestRelease = _newerReleases[0];
            var latestVersion = VersionChecker.ParseReleaseVersion(latestRelease.TagName);
            _logger.Information($"Updating AssetEditor from version {currentVersion} to version {latestVersion}");

            DeleteUpdateDirectory();

            LaunchUpdater();

            Application.Current.Shutdown();
        }

        public static void DeleteUpdateDirectory()
        {
            var updateDirectory = DirectoryHelper.UpdateDirectory;
            if (Directory.Exists(updateDirectory))
                Directory.Delete(updateDirectory, true);
        }

        public static void LaunchUpdater()
        {
            var currentDirectory = AppContext.BaseDirectory;
            var updaterPath = Path.Combine(currentDirectory, AssetEditorUpdaterExe);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                WorkingDirectory = currentDirectory,
                UseShellExecute = true,
            };

            if (UpdaterRequiresAdministratorPrivileges())
                processStartInfo.Verb = "runas";

            Process.Start(processStartInfo);
        }

        public static bool UpdaterRequiresAdministratorPrivileges()
        {
            try
            {
                var currentDirectory = AppContext.BaseDirectory;
                var testFilePath = Path.Combine(currentDirectory, $"updater_admin_privileges_test{Guid.NewGuid():N}.tmp");

                using (File.Create(testFilePath, 1, FileOptions.DeleteOnClose))
                {
                }

                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();


        public void SetCloseAction(Action closeAction) => _closeAction = closeAction;
    }
}
