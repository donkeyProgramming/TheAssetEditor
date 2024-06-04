using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommunityToolkit.Diagnostics;
using System;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActionGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(Action);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as Action;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project.ProjectSettings.BnkName, project);
        }

        public CAkAction_v136 ConvertToWWise(Action inputAction, string bnkName, CompilerData project)
        {
            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = project.GetHircItemIdFromName(inputAction.Name);
            wwiseAction.Type = HircType.Action;
            wwiseAction.ActionType = ActionType.Play;
            wwiseAction.idExt = project.GetHircItemIdFromName(inputAction.ChildId);

            wwiseAction.AkPlayActionParams.byBitVector = 0x04;
            wwiseAction.AkPlayActionParams.bankId = project.ConvertStringToWWiseId(bnkName);

            wwiseAction.UpdateSize();
            return wwiseAction;
        }
    }
}
