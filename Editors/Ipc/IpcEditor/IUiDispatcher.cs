using System.Windows;
using System.Windows.Threading;

namespace Editors.Ipc
{
    public interface IUiDispatcher
    {
        Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken);
    }

    internal sealed class WpfUiDispatcher : IUiDispatcher
    {
        public async Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                return action();

            return await dispatcher.InvokeAsync(action, DispatcherPriority.Normal, cancellationToken);
        }
    }
}
