using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components.Component
{
    class ObjectEditor : BaseComponent
    {
        public ObjectEditor(WpfGame game) : base(game)
        { }

        // DeleteObject = del
        // Duplicate object= ctrl+d
    }


    class FaceEditor : BaseComponent
    {

        public FaceEditor(WpfGame game) : base(game)
        { }

        // Delet faces= del
        // Create new object using selected faces = ctrl+d
    }
}
