using Editors.Audio.AudioProjectCompiler.WwiseIDService.Warhammer3;
using Shared.Core.Settings;

namespace Editors.Audio.AudioProjectCompiler.WwiseIDService
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
