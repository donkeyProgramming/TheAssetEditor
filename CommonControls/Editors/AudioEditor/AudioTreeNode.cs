using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public class AudioTreeNode
    {
        public NotifyAttr<string> Name { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> Guid { get; set; } = new NotifyAttr<string>();
        public string XmlContent { get; set; }
        public ObservableCollection<AudioTreeNode> Children { get; set; } = new ObservableCollection<AudioTreeNode>();
    }
}
