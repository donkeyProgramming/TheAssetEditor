using CommonControls.PackFileBrowser;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Common
{
    public interface IDropTarget<T>
    {
        bool AllowDrop(T node, T targeNode = default);
        bool Drop(T node, T targeNode = default);
    }

    public interface IDropTarget<T, U>
    {
        bool AllowDrop(T node, T targeNode = default, U additionalData = default);
        bool Drop(T node, T targeNode = default, U additionalData = default);
    }
}
