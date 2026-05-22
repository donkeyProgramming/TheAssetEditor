# 01 — Create a new Animation applet (editor)

This guide shows how to add a new Animation applet (editor) following the same structure as
`AnimationKeyframeEditorViewModel` and `MountAnimationCreatorViewModel`.

Where to put code
- Place your editor-specific files under `Editors/AnimationEditor/<YourAppletName>/` (this is consistent with the existing editors).
- Example existing editors:
  - `Editors/AnimationEditor/AnimationKeyframeEditor/AnimationKeyframeEditorViewModel.cs`
  - `Editors/AnimationEditor/MountAnimationCreator/MountAnimationCreatorViewModel.cs`

High level steps
1. Create a ViewModel class (recommended: derive from `EditorHostBase`).
2. (Optional) Create a custom view `UserControl` if you want a custom UI; otherwise the shared `EditorHostView` is used.
3. Add a `Create(...)` method and wire your SceneObjects and services exactly like the existing editors.
4. Add any helper classes (ViewModels, sub-ViewModels) under your folder.

Minimal ViewModel skeleton (copy & adapt)

```csharp
// file: Editors/AnimationEditor/MyApplet/MyAppletViewModel.cs
using System;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Shared.Core.ToolCreation;
using Microsoft.Extensions.DependencyInjection; // (for DI registration example later)

namespace Editors.AnimationVisualEditors.MyApplet
{
    public partial class MyAppletViewModel : EditorHostBase
    {
        public override Type EditorViewModelType => typeof(AnimationEditor.Common.BaseControl.EditorHostView);

        // Dependencies you typically need — match constructor to what DI can provide.
        public MyAppletViewModel(
            IEditorHostParameters editorHostParameters,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            SceneObjectEditor sceneObjectEditor,
            IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            SelectionManager selectionManager,
            IFileSaveService fileSaveService,
            IUiCommandFactory uiCommandFactory) : base(editorHostParameters)
        {
            DisplayName = "My Applet";

            // Use the provided builders/services to create default scene objects the same way
            // other editors do to ensure things like MountLinkController are initialized.
            var riderItem = sceneObjectViewModelBuilder.CreateAsset("IDK", true, "Rider", Microsoft.Xna.Framework.Color.Black, null);
            var mountItem = sceneObjectViewModelBuilder.CreateAsset("IDK", true, "Mount", Microsoft.Xna.Framework.Color.Black, null);
            mountItem.Data.IsSelectable = true;

            var newAnimAsset = sceneObjectEditor.CreateAsset("IDK", "New Anim", Microsoft.Xna.Framework.Color.Red);
            animationPlayerViewModel.RegisterAsset(newAnimAsset);

            Create(riderItem.Data, mountItem.Data, newAnimAsset);
            SceneObjects.Add(riderItem);
            SceneObjects.Add(mountItem);
        }

        internal void Create(SceneObject rider, SceneObject mount, SceneObject newAnimation)
        {
            // store refs, hook events, create sub-viewmodels etc.
        }
    }
}
```

Notes about using `EditorHostBase`
- The shared `EditorHostView` is used by many editors and is a good default unless you need custom UI.
- Put logic that wires SceneObjects and registers with the `AnimationPlayerViewModel` into the constructor, or into a helper `Create(...)` method similar to existing editors.

Common pitfalls
- If you leave essential components (like the MountLink controller in the Mount editor) uncreated until some manual initialization, code that expects them on startup can get NullReferenceExceptions. The pattern in the repo is to create at least default Rider/Mount/NewAnim scene objects in the constructor so the editor is ready.
- Be mindful of the non-nullable reference warnings. You can:
  - initialize fields eagerly,
  - mark them nullable if they legitimately start null, or
  - ensure constructors always initialize them.

Testing
- Build the solution. If there are DI or registration errors the build/run will tell you which types are missing.
- Next step is to register the applet in DI and the editor database (see `02-register-and-debug.md`).

References
- Example ViewModels to copy from:
  - `Editors/AnimationEditor/AnimationKeyframeEditor/AnimationKeyframeEditorViewModel.cs`
  - `Editors/AnimationEditor/MountAnimationCreator/MountAnimationCreatorViewModel.cs`

--
