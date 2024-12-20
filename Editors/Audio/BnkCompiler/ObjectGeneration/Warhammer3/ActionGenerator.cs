using System;
using CommunityToolkit.Diagnostics;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.Utility;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc.V136;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
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

        public static CAkAction_v136 ConvertToWWise(Action inputAction, CompilerData project)
        {
            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = inputAction.Id;
            wwiseAction.Type = HircType.Action;
            wwiseAction.ActionType = ActionType.Play;
            wwiseAction.IdExt = inputAction.ChildId;
            wwiseAction.AkPlayActionParams.ByBitVector = 0x04;
            wwiseAction.AkPlayActionParams.BankId = WwiseHash.Compute(project.ProjectSettings.BnkName);
            
            wwiseAction.UpdateSize();
            return wwiseAction;
        }
    }
}
