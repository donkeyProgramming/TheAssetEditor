using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components
{
    public enum ComponentUpdateOrderEnum
    {
        Input,
        Camera,
        Gizmo,
        PickingComponent,
        Default,
    }

    public enum ComponentDrawOrderEnum
    {
        Gizmo,
        PickingComponent,
        Default,
    }
}
