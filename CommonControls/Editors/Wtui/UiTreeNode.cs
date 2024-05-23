// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using SharedCore.Misc;

namespace CommonControls.Editors.Wtui
{
    public class UiTreeNode
    {
        public NotifyAttr<string> Name { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> Guid { get; set; } = new NotifyAttr<string>();
        public string XmlContent { get; set; }
        public ObservableCollection<UiTreeNode> Children { get; set; } = new ObservableCollection<UiTreeNode>();
    }
}
