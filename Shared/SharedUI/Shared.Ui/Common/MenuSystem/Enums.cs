namespace Shared.Ui.Common.MenuSystem
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
