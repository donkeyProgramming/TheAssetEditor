using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AssetEditor.Services
{
    class VersionChecker
    {
        public static void CheckVersion(string expectedTagName)
        {
            try
            {

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                GitHubClient client = new GitHubClient(new ProductHeaderValue("AssetEditor_instance"));
                IReadOnlyList<Release> releases = client.Repository.Release.GetAll("olekristianhomelien", "TheAssetEditor").Result;
            
                var latest = releases.FirstOrDefault();
                if (!latest.TagName.Contains(expectedTagName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MessageBox.Show("You are using an old version, please go to\nhttps://github.com/olekristianhomelien/TheAssetEditor/releases/latest\nto download " + latest.TagName);
                }
            }
            catch
            {
                MessageBox.Show("Unbale to contact Github to check for later version");
            }
        }
    }
}
