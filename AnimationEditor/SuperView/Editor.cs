using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AnimationEditor.SuperView
{
    public class Editor
    {

        public ObservableCollection<object> MyItems { get; set; } = new ObservableCollection<object>();

        public Editor()
        {
            MyItems.Add(null);
            MyItems.Add(null);
        }

    }
}
