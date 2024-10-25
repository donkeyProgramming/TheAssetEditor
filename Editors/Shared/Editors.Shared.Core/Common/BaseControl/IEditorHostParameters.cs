using Editors.Shared.Core.Common.AnimationPlayer;
using GameWorld.Core.Components;
using GameWorld.Core.Services;
using Shared.Core.Services;

namespace Editors.Shared.Core.Common.BaseControl
{
    public interface IEditorHostParameters
    {
        AnimationPlayerViewModel AnimationPlayerViewModel { get; }
        IComponentInserter ComponentInserter { get; }
        FocusSelectableObjectService FocusSelectableObjectService { get; }
        IWpfGame GameWorld { get; }
        SceneObjectViewModelBuilder SceneObjectViewModelBuilder { get; }
        SceneObjectEditor SceneObjectEditor { get; }
    }

    public class EditorHostParameters : IEditorHostParameters
    {
        public EditorHostParameters(
            IComponentInserter componentInserter,
            AnimationPlayerViewModel animationPlayerViewModel,
            IWpfGame gameWorld,
            FocusSelectableObjectService focusSelectableObjectService,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            SceneObjectEditor sceneObjectEditor)
        {
            ComponentInserter = componentInserter;
            AnimationPlayerViewModel = animationPlayerViewModel;
            GameWorld = gameWorld;
            FocusSelectableObjectService = focusSelectableObjectService;
            SceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            SceneObjectEditor = sceneObjectEditor;
        }

        public IComponentInserter ComponentInserter { get; }
        public AnimationPlayerViewModel AnimationPlayerViewModel { get; }
        public IWpfGame GameWorld { get; }
        public FocusSelectableObjectService FocusSelectableObjectService { get; }
        public SceneObjectViewModelBuilder SceneObjectViewModelBuilder { get; }

        public SceneObjectEditor SceneObjectEditor { get; }
    }
}
