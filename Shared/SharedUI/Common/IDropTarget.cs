// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Shared.Ui.Common
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
