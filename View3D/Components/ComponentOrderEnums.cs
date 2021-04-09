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
        Default,
        
        RenderEngine,
        Gizmo,
        SelectionComponent,
    }
}
