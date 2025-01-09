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

        public static CAkAction_V136 ConvertToWwise(Action inputAction, CompilerData project)
        {
            var wwiseAction = new CAkAction_V136
            {
                Id = inputAction.Id,
                HircType = AkBkHircType.Action,
                ActionType = AkActionType.Play,
                IdExt = inputAction.ChildId,

                PlayActionParams = new CAkAction_V136.PlayActionParams_V136
                {
                    BitVector = 0x04,
                    BankId = WwiseHash.Compute(project.ProjectSettings.BnkName)
                }
            };

            wwiseAction.UpdateSectionSize();

            return wwiseAction;
        }
    }
}
