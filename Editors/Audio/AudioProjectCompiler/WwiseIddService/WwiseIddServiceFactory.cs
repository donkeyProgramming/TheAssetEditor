using Editors.Audio.AudioProjectCompiler.WwiseIdService.Warhammer3;
using Shared.Core.Settings;

namespace Editors.Audio.AudioProjectCompiler.WwiseIdService
{
    public class WwiseIddServiceFactory
    {
        public static IWwiseIddService GetWwiseIdService(GameTypeEnum game)
        {
            return game switch
            {
                GameTypeEnum.Warhammer3 => new Wh3WwiseIddService()
            };
        }
    }
}
