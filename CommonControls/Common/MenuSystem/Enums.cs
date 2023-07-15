// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    public enum ButtonVisibilityRule
    {
        Always,
        ObjectMode,
        FaceMode,
        VertexMode,
    }
}
