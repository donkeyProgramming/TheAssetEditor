// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using CommonControls.Services;

namespace CommonControls.PackFileBrowser
{
    public class OpenFileContexMenuHandler : ContextMenuHandler
    {
        public OpenFileContexMenuHandler(PackFileService service) : base(service, null)
        { }

        public override void Create(TreeNode node)
        {
            _selectedNode = node;
            Items = new ObservableCollection<ContextMenuItem>();
            if (node == null)
                return;

            if (node.NodeType != NodeType.File)
            {
                Additem(ContextItems.Expand, Items);
                Additem(ContextItems.Collapse, Items);
                AddSeperator(Items);
                Additem(ContextItems.CreateFolder, Items);
            }
        }
    }
}
