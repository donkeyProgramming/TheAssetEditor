using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace AssetEditor.Services
{
    class VersionChecker
    {
        public static void CheckVersion()
        {
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

                var expectedVersion = "v" + fvi.FileMajorPart + "." + fvi.FileMinorPart;

                GitHubClient client = new GitHubClient(new ProductHeaderValue("AssetEditor_instance"));
                IReadOnlyList<Release> releases = client.Repository.Release.GetAll("olekristianhomelien", "TheAssetEditor").Result;
            
                var latest = releases.FirstOrDefault();
                if (!latest.TagName.Contains(expectedVersion, StringComparison.InvariantCultureIgnoreCase))
                {
                    var githubLink = @"https://github.com/olekristianhomelien/TheAssetEditor/releases/latest";
                    var res = MessageBox.Show("You are using an old version, please go to\nhttps://github.com/olekristianhomelien/TheAssetEditor/releases/latest\nto download \nGo there now?", "Version checker", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        OpenUrl(githubLink);
                    }
                
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Unable to contact Github to check for later version");
            }
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
