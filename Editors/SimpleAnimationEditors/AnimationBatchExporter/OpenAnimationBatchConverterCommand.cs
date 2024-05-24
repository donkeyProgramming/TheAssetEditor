using Shared.Core.Events;
using Shared.Ui.BaseDialogs;

namespace CommonControls.Editors.AnimationBatchExporter;

public class OpenAnimationBatchConverterCommand : IUiCommand
{
    private readonly IWindowFactory _windowFactory;

    public OpenAnimationBatchConverterCommand(IWindowFactory windowFactory)
    {
        _windowFactory = windowFactory;
    }

    public void Execute()
    {
        var window = _windowFactory.Create<AnimationBatchExportViewModel, AnimationBatchExportView>("Animation batch converter", 400, 300);
        window.AlwaysOnTop = true;
        window.ShowWindow();
    }
}
