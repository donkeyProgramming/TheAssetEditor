using Editors.Audio.AudioProjectCompiler.WwiseIdService.Warhammer3;
using Shared.Core.Settings;

namespace Editors.Audio.AudioProjectCompiler.WwiseIdService
{
    public class WwiseIdServiceFactory
    {
        public static IWwiseIdService GetWwiseIdService(GameTypeEnum game)
        {
            return game switch
            {
                GameTypeEnum.Warhammer3 => new Wh3WwiseIdService()
            };
        }
    }
}
