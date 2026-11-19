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
            // "cmd /c start" split paths on spaces and mangled URLs containing '&'
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
    }
}
