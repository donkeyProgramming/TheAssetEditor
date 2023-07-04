namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public enum MenuActionType
    { 
        Save,
        SaveAs,
        GenerateWsModelForWh3,
        GenerateWsModelForWh2,
        OpenImportReference,
        ImportReferencePaladin,
        ImportReferenceSlayer,
        ImportReferenceGoblin,
        ImportMapForDebug,
        DeleteLods,
        ClearConsole,
        SortModelsByName,

        Group,
        Undo,
        Gizmo_ScaleUp,
        Gizmo_ScaleDown,
        Gizmo_Arrow,
        Gizmo_Move,
        Gizmo_Rotate,
        Gizmo_Scale,

        SelectObject,
        SelectFace,
        SelectVertex,
        SelectBone,

        ViewOnlySelected,
        FocusSelection,
        ResetCamera,
        ToogleBackFaceRendering,
        ToggleLargeSceneRendering,

        DevideToSubmesh,
        DevideToSubmesh_withoutCombining,
        MergeSelectedMeshes,
        DuplicateSelected,
        DeleteSelected,
        ConvertSelectedMeshIntoStaticAtCurrentAnimFrame,

        ReduceMesh10x,
        CreateLod,
        OpenBmiTool,
        OpenSkeletonResharper,
        OpenReRiggingTool,
        OpenPinTool,
        OpenVertexDebuggerTool,

        GrowFaceSelection,
        ConvertFaceToVertexSelection,
        CopyLod0ToEveryLodSlot,
        UpdateWh2Model_Technique1,
        UpdateWh2Model_Technique2
    }
}
