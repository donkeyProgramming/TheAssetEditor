using System.Collections.Generic;
using System.Linq;
using CommonControls.BaseDialogs;
using CommonControls.Editors.BoneMapping;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.MeshFitter
{
    internal class ReRiggingViewModel : BoneMappingViewModel
    {
        private readonly CommandFactory _commandFactory;
        private List<Rmv2MeshNode> _selectedMeshes;

        public ReRiggingViewModel(CommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        internal void Initialize(List<Rmv2MeshNode> selectedMeshes, ITypedAssetEditorWindow<ReRiggingViewModel> window, RemappedAnimatedBoneConfiguration config)
        {
            _selectedMeshes = selectedMeshes;
            Initialize(window, config);
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
                errorText = "Not all bones mapped. This will not work as you expect and will case problems later!\nOnly do this if your REALLY know what you are doing";
                return false;
            }
            errorText = "";
            return true;
        }
    }
}
