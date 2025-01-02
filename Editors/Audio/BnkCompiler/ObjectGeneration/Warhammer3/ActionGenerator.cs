using CommunityToolkit.Diagnostics;
using System;
using Shared.GameFormats.Wwise.Hirc.V136;
using Editors.Audio.BnkCompiler;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActionGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(Action);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as Action;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public static CAkAction_v136 ConvertToWwise(Action inputAction, CompilerData project)
        {
            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = inputAction.Id;
            wwiseAction.HircType = AkBkHircType.Action;
            wwiseAction.ActionType = AkActionType.Play;
            wwiseAction.IdExt = inputAction.ChildId;
            wwiseAction.PlayActionParams.BitVector = 0x04;
            wwiseAction.PlayActionParams.BankId = WwiseHash.Compute(project.ProjectSettings.BnkName);
            wwiseAction.UpdateSectionSize();
            return wwiseAction;
        }
    }
}
