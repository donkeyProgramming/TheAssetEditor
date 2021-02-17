using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components
{
    public enum ComponentUpdateOrderEnum
    {
        RenderEngine,
        Input,
        Camera,
        Gizmo,
        SelectionComponent,
        Default,
    }

    public enum ComponentDrawOrderEnum
    {
        ClearScreenComponent,
        Gizmo,
        Default,
        RenderEngine,
        SelectionComponent,
    }
}
