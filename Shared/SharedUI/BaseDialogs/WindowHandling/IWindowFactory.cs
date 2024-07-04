namespace Shared.Ui.BaseDialogs.WindowHandling
{
    public interface IWindowFactory
    {
        AssetEditorWindow<TViewModel> Create<TViewModel, TView>(string title, int initialWidth, int initialHeight) where TViewModel : class;
    }
}
