# 02 — Register your applet in DI and add a dev-config for quick debug

After you add your ViewModel (see `01-create-applet.md`), register it so the editor host can create and show it.

1) Add the ViewModel to the animation editors DI container

Edit the container at:

```
TheAssetEditor/Editors/AnimationEditor/DependencyInjectionContainer.cs
```

Inside `Register(IServiceCollection serviceCollection)` add a scoped registration for your view model:

```csharp
serviceCollection.AddScoped<MyAppletViewModel>();
```

This mirrors how the repo registers existing editors:

```csharp
serviceCollection.AddScoped<MountAnimationCreatorViewModel>();
serviceCollection.AddScoped<AnimationKeyframeEditorViewModel>();
```

2) Register the editor in the editor database (so it appears in the toolbar/menu)

In the same `DependencyInjectionContainer`, implement `RegisterTools(IEditorDatabase database)` and add an EditorInfo entry. Example:

```csharp
EditorInfoBuilder
    .Create<MyAppletViewModel, AnimationEditor.Common.BaseControl.EditorHostView>(EditorEnums.MyApplet_Editor)
    .AddToToolbar("My Applet", true)
    .Build(database);
```

Notes:
- `EditorHostView` is the default shared host view. Use a custom UserControl type here if you created a custom UI view for your applet.
- `EditorEnums.MyApplet_Editor` must exist in the `EditorEnums` enum (see next step).

3) Add an enum value for your editor

Open:

```
Shared/SharedCore/Shared.Core/ToolCreation/EditorEnums.cs
```

Add an entry (for example):

```csharp
public enum EditorEnums
{
    ...
    MyApplet_Editor,
    None,
}
```

Make sure the enum value is unique and update any usages if necessary.

4) (Optional) Add a dev-config so your app opens pre-configured for development

To open your editor automatically (with sample inputs), add a dev-config like the repo's `MountTool` example. Create a class under `Editors/AnimationEditor/MyApplet/DevConfig/MyAppletDevConfig.cs` implementing `IDeveloperConfiguration`.

Sample DevConfig (copy & adapt from `MountTool`):

```csharp
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using Shared.Core.PackFiles; // for packfile lookups

namespace Editors.AnimationVisualEditors.MyApplet.DevConfig
{
    internal class MyAppletDevConfig : IDeveloperConfiguration
    {
        private readonly IEditorManager _editorManager;
        private readonly IPackFileService _packFileService;

        public MyAppletDevConfig(IEditorManager editorManager, IPackFileService packFileService)
        {
            _editorManager = editorManager;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            // if you need to change game settings for development/testing
        }

        public void OpenFileOnLoad()
        {
            // create a debug AnimationToolInput (like MountTool does)
            var input = new AnimationToolInput()
            {
                Mesh = _packFileService.FindFile("variantmeshes\\...\\some.variantmeshdefinition"),
                Animation = _packFileService.FindFile("animations\\...\\some.anim")
            };

            _editorManager.Create(EditorEnums.MyApplet_Editor, x => (x as MyAppletViewModel)?.SetDebugInputParameters(input));
        }
    }
}
```

This is useful for quickly launching your editor preloaded with assets.

5) Build and run

- Build the solution and run the host application.
- The editor toolbar should show your applet (if you used `.AddToToolbar(...)`).
- Use the toolbar button or the editor manager to create your editor.

Troubleshooting
- If constructor dependencies are missing, ensure the required services are registered in other DI containers that the top-level bootstrapping includes (the project has many specialized DI containers; the Animation DI container depends on other shared containers).
- If your editor throws NREs at startup related to missing controllers, follow the pattern in `MountAnimationCreatorViewModel` and create default Rider/Mount/NewAnim objects in the constructor so dependent controllers are created immediately.

Extra: register a custom view
- If you create a custom `UserControl` for your applet (e.g. `MyAppletView`), register it (AddTransient) in the DI container and change the EditorInfo registration to:

```csharp
EditorInfoBuilder
    .Create<MyAppletViewModel, MyAppletView>(EditorEnums.MyApplet_Editor)
    .AddToToolbar("My Applet", true)
    .Build(database);
```

That's it — you should now be able to add and iterate on an applet using the same patterns as the repo's keyframe and mount editors.
