using System;
using System.Collections.Generic;
using System.Text;

namespace Common.GameInformation
{

    public class GameInformation
    {
        public GameTypeEnum Type { get; set; }
        public string DisplayName { get; set; }
        public string DefaultPackType { get; set; }
    }

    public static class GameInformationFactory
    { 
        public static GameInformation Warhammer { get; private set; }
        public static GameInformation Warhammer2 { get; private set; }

        public static List<GameInformation> Games { get; private set; }

        public static void Create()
        {
            Warhammer = new GameInformation() { Type = GameTypeEnum.Warhammer1, DisplayName = "Warhammer", DefaultPackType = "PFH4" };
            Warhammer2 = new GameInformation() { Type = GameTypeEnum.Warhammer2, DisplayName = "Warhammer II", DefaultPackType = "PFH5" };
            Games = new List<GameInformation>() { Warhammer, Warhammer2 };
        }

    }
}
