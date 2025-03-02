using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService.Warhammer3;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService
{
    public class WwiseIDServiceFactory
    {
        public static IWwiseIDService GetWwiseIDService(GameTypeEnum game)
        {
            return game switch
            {
                GameTypeEnum.Warhammer3 => new Wh3WwiseIDService()
            };
        }
    }
}
