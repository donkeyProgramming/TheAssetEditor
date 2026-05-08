using Shared.Core.Services;
using Shared.Ui.Common;

namespace Shared.Ui.BaseDialogs.StandardDialog
{
    public class AeWaitCursor : IWaitCursor
    {
        readonly WaitCursor _handle;
        bool _isDisposed;
        public AeWaitCursor()
        {
            _handle = new WaitCursor();
        }

        public void Dispose()
        {
            if (_isDisposed == false)
                _handle.Dispose();
            _isDisposed = true;
        }
    }
}
