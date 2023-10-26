using Octokit;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace AssetEditor.Services
{
    class VersionChecker
    {
        private static readonly string GitHubLink = @"https://github.com/olekristianhomelien/TheAssetEditor/releases/latest";

        public static void CheckVersion()
        {
            if (Debugger.IsAttached)
                return;

            try
            {
                //System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                // FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                

                var currentVersion = "v" + fvi.FileMajorPart + "." + fvi.FileMinorPart;
                GitHubClient client = new GitHubClient(new ProductHeaderValue("AssetEditor_instance"));

                client.Repository.Release.GetAll("olekristianhomelien", "TheAssetEditor").ContinueWith(
                    task =>
                    {
                        try
                        {
                            if (task.IsFaulted)
                            {
                                Exception ex = task.Exception;
                                while (ex is AggregateException && ex.InnerException != null)
                                    ex = ex.InnerException;
                                if (ex != null)
                                    throw ex;
                            }

                            var releases = task.Result;
                            var latest = releases.FirstOrDefault();
                            if (!latest.TagName.Contains(currentVersion, StringComparison.InvariantCultureIgnoreCase))
                            {
                                ProcessMessage(latest, currentVersion);
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Unable to contact Github to check for later version");
                        }
                    }
                );
            }
            catch
            {
                MessageBox.Show("Unable to contact Github to check for later version");
            }
        }

        private static void ProcessMessage(Release lastRelease, string currentVersion)
        {
            var changes = "\n" + lastRelease.Body;
            var changesIndented = changes.Replace("\n", "\n\t");
            var message = $"You are using an old version {currentVersion}, please go to\n{GitHubLink} to download {lastRelease.TagName} \n\nChanges:{changesIndented}\n\nGo to download page now?";

            var messageBoxResult = MessageBox.Show(message, "Version checker", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
                OpenUrl(GitHubLink);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
