using CommunityToolkit.Diagnostics;
using System;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc.V136;
using Audio.Utility;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActionGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(Action);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as Action;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkAction_v136 ConvertToWWise(Action inputAction, CompilerData project)
        {
            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = inputAction.Id;
            wwiseAction.Type = HircType.Action;
            wwiseAction.ActionType = ActionType.Play;
            wwiseAction.idExt = inputAction.ChildId;
            wwiseAction.AkPlayActionParams.byBitVector = 0x04;
            wwiseAction.AkPlayActionParams.bankId = WwiseHash.Compute(project.ProjectSettings.BnkName);
            wwiseAction.UpdateSize();

            return wwiseAction;
        }
    }
}
