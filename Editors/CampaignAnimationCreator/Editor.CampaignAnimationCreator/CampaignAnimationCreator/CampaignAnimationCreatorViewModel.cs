using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Ui.Common;

namespace Editor.CampaignAnimationCreator.CampaignAnimationCreator
{
    public partial class CampaignAnimationCreatorViewModel : EditorHostBase
    {
        private SceneObject? _sceneObject;
        private AnimationClip? _selectedAnimationClip;
        private readonly IUiCommandFactory _uiCommandFactory;

        public override Type EditorViewModelType => typeof(EditorView);
        public FilterCollection<SkeletonBoneNode> ModelBoneList { get; } = new(null);

        public CampaignAnimationCreatorViewModel(IEditorHostParameters editorHostParameters, IUiCommandFactory uiCommandFactory)
            : base(editorHostParameters)
        {
            DisplayName = "CampaignAnimationCreator";
            _uiCommandFactory = uiCommandFactory;
            Initialize();
        }

        private void Initialize()
        {
            var item = _sceneObjectViewModelBuilder.CreateAsset("model", true, "model", Color.Black, null);
            InitializeSelectedUnit(item.Data);
            SceneObjects.Add(item);
        }

        private void InitializeSelectedUnit(SceneObject sceneObject)
        {
            _sceneObject = sceneObject;
            _sceneObject.SkeletonChanged += SkeletonChanged;
            _sceneObject.AnimationChanged += AnimationChanged;

            SkeletonChanged(_sceneObject.Skeleton);
            AnimationChanged(_sceneObject.AnimationClip);
        }

        [RelayCommand]
        public void ConvertAction()
        {
            Guard.IsNotNull(_sceneObject, "Scene object not created - unable to convert");

            var result = _uiCommandFactory.Create<ConvertCampaignAnimationCommand>().Execute(_selectedAnimationClip, ModelBoneList.SelectedItem, out var convertedAnimation);
            if (result == false)
                return;
            
            var outputName = _sceneObject.AnimationName.Value;
            SceneObjectEditor.SetAnimationClip(_sceneObject, convertedAnimation, outputName);
        }

        [RelayCommand]
        public void SaveAnimationAction()
        {
            Guard.IsNotNull(_sceneObject, "Scene object not created - unable to save");

            _uiCommandFactory.Create<SaveCampaignAnimationCommand>().Execute(_sceneObject.Skeleton, _sceneObject.AnimationClip);
        }

        private void AnimationChanged(AnimationClip? newValue) => _selectedAnimationClip = newValue;
        
        private void SkeletonChanged(GameSkeleton? newValue)
        {
            if (newValue == null)
            {
                ModelBoneList.UpdatePossibleValues(null);
                return;
            }

            ModelBoneList.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));
            ModelBoneList.SelectedItem = ModelBoneList.PossibleValues.FirstOrDefault(x => string.Equals(x.BoneName, "animroot", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
