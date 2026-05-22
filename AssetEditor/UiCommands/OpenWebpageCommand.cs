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
            Process.Start(new ProcessStartInfo("cmd", $"/c start {_url}") { CreateNoWindow = true });
        }
    }
}
