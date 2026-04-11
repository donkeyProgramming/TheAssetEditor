using GameWorld.Core.Commands;
using GameWorld.Core.SceneNodes;
using Shared.Core.Services;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.ChildEditors.ReRiggingTool
{
    public class ReRiggingViewModel : BoneMappingViewModel
    {
        private readonly CommandFactory _commandFactory;
        private List<Rmv2MeshNode> _selectedMeshes;

        public ReRiggingViewModel(CommandFactory commandFactory, LocalizationManager localizationManager) : base(localizationManager)
        {
            _commandFactory = commandFactory;
        }

        public void Initialize(List<Rmv2MeshNode> selectedMeshes, RemappedAnimatedBoneConfiguration config)
        {
            _selectedMeshes = selectedMeshes;
            Initialize(config);
        }

        protected override void ApplyChanges()
        {
            var remapping = AnimatedBoneHelper.BuildRemappingList(_configuration.MeshBones.First());
            _commandFactory.Create<RemapBoneIndexesCommand>().Configure(x => x.Configure(_selectedMeshes, remapping, _configuration.ParnetModelSkeletonName)).BuildAndExecute();
        }

        public override bool Validate(out string errorText)
        {
            var usedBonesCount = AnimatedBoneHelper.GetUsedBonesCount(MeshBones.PossibleValues.First());
            var mapping = AnimatedBoneHelper.BuildRemappingList(MeshBones.PossibleValues.First());
            var numMappings = mapping.Count(x => x.IsUsedByModel);
            if (usedBonesCount != numMappings)
            {
                errorText = _localizationManager.Get("KitbasherTool.BoneMapping.ValidationIncompleteMapping");
                return false;
            }
            errorText = "";
            return true;
        }
    }
}
