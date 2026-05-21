using System.Diagnostics;
using Shared.Core.Events;

namespace AssetEditor.UiCommands
{
    public class OpenWebpageCommand : IAeCommand
    {
        public OpenWebpageCommand()
        {
        }

        public void Execute(string url)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
