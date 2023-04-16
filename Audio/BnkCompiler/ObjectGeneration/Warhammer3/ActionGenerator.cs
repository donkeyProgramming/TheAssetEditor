using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using Action = CommonControls.Editors.AudioEditor.BnkCompiler.Action;
using System.Linq;
using System.Collections.Generic;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActionGenerator
    {
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

        public List<CAkAction_v136> ConvertToWWise(IEnumerable<Action> inputAction, string bnkName, HircProjectItemRepository repository)
        {
            return inputAction.Select(x => ConvertToWWise(x, bnkName, repository)).ToList();
        }
    }
}
