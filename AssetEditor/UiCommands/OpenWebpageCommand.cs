using System.Diagnostics;
using Shared.Core.Events;

namespace AssetEditor.UiCommands
{
    public class OpenWebpageCommand : IAeCommand
    {
        private string _url = string.Empty;

        public OpenWebpageCommand()
        {
        }

        public void Configure(string url)
        {
            _url = url;
        }

        public void Execute()
        {
            // Let the shell resolve the target directly. Going through "cmd /c start" split
            // local paths on spaces (e.g. C:\Program Files\...) and mangled URLs containing '&'.
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
    }
}
