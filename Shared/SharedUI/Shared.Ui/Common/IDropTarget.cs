namespace Shared.Ui.Common
{
    public interface IDropTarget<T>
    {
        bool AllowDrop(T node, T? targeNode = default);
        bool Drop(T node, T? targeNode = default);
    }

    public interface IDropTarget<T, U>
    {
        bool AllowDrop(T node, T? targeNode = default, U? additionalData = default);
        bool Drop(T node, T? targeNode = default, U? additionalData = default);
    }
}
