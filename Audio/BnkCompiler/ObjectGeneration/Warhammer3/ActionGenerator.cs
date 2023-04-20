using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using Action = CommonControls.Editors.AudioEditor.BnkCompiler.Action;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System;
using CommunityToolkit.Diagnostics;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActionGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(Action);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, AudioInputProject project, HircProjectItemRepository repository)
        {
            var typedProjectItem = projectItem as Action;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project.ProjectSettings.BnkName, repository);
        }

        public CAkAction_v136 ConvertToWWise(Action inputAction, string bnkName, HircProjectItemRepository repository)
        {
            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = repository.GetHircItemId(inputAction.Id);
            wwiseAction.Type = HircType.Action;
            wwiseAction.ActionType = ActionType.Play;
            wwiseAction.idExt = repository.GetHircItemId(inputAction.ChildId);

            wwiseAction.AkPlayActionParams.byBitVector = 0x04;
            wwiseAction.AkPlayActionParams.bankId = repository.ConvertStringToWWiseId(bnkName);

            wwiseAction.UpdateSize();
            return wwiseAction;
        }
    }
}
