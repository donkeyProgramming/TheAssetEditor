using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Shared.Core.Services;

namespace AssetEditor.Services.Ipc
{
    public class IpcUserNotifier : IIpcUserNotifier
    {
        private readonly IStandardDialogs _standardDialogs;

        public IpcUserNotifier(IStandardDialogs standardDialogs)
        {
            _standardDialogs = standardDialogs;
        }

        public async Task ShowExternalOpenFailedAsync(string normalizedPath, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var message = $"External open failed: {normalizedPath}";
            var app = Application.Current;
            if (app?.Dispatcher == null)
            {
                _standardDialogs.ShowDialogBox(message, "AssetEditor IPC");
                return;
            }

            if (app.Dispatcher.CheckAccess())
            {
                _standardDialogs.ShowDialogBox(message, "AssetEditor IPC");
                return;
            }

            await app.Dispatcher.InvokeAsync(() => _standardDialogs.ShowDialogBox(message, "AssetEditor IPC"));
        }
    }
}
