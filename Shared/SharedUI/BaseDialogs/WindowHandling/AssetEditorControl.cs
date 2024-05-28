using System;
using System.Windows.Controls;

namespace Shared.Ui.BaseDialogs.WindowHandling
{
    public class AssetEditorControl : UserControl
    {
        public event EventHandler RequestClose;
        public event EventHandler RequestOK;

        public void TriggerRequestClose() => RequestClose?.Invoke(this, EventArgs.Empty);
        public void TriggerRequestOk() => RequestOK?.Invoke(this, EventArgs.Empty);
    }
}
