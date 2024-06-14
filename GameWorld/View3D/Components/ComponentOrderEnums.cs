namespace GameWorld.Core.Components
{
    public enum ComponentUpdateOrderEnum
    {
        RenderEngine,
        Input,
        Camera,
        Animation,

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
