using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Common.MenuSystem
{
    public enum ActionEnabledRule
    {
        Always,
        OneObjectSelected,
        AtleastOneObjectSelected,
        TwoObjectesSelected,
        TwoOrMoreObjectsSelected,
        FaceSelected,
        VertexSelected,
        AnythingSelected,
        ObjectOrVertexSelected,
        ObjectOrFaceSelected,
        Custom
    }

    public enum ButtonVisabilityRule
    {
        Always,
        ObjectMode,
        FaceMode,
        VertexMode,
    }
}
