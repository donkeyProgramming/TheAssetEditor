using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.Services.GameInformation
{

    public class GameInformation
    {
        public GameTypeEnum Type { get; set; }
        public string DisplayName { get; set; }
        public string DefaultPackType { get; set; }
        public string ShortID { get; set; }
    }

    public static class GameInformationFactory
    {
        public static GameInformation Warhammer { get; private set; }
        public static GameInformation Warhammer2 { get; private set; }
        public static GameInformation Troy { get; private set; }
        public static GameInformation ThreeKingdoms { get; private set; }
        public static GameInformation Rome2Remastered { get; private set; }
        public static GameInformation Attilla { get; private set; }
        public static List<GameInformation> Games { get; private set; }

        public static void Create()
        {
            Warhammer = new GameInformation() { Type = GameTypeEnum.Warhammer1, DisplayName = "Warhammer", DefaultPackType = "PFH4", ShortID = "warhammer" };
            Warhammer2 = new GameInformation() { Type = GameTypeEnum.Warhammer2, DisplayName = "Warhammer II", DefaultPackType = "PFH5", ShortID = "warhammer2" };
            Troy = new GameInformation() { Type = GameTypeEnum.Troy, DisplayName = "Troy", DefaultPackType = "PFH5", ShortID = "troy" };
            ThreeKingdoms = new GameInformation() { Type = GameTypeEnum.ThreeKingdoms, DisplayName = "Three Kingdoms", DefaultPackType = "PFH5", ShortID = "ThreeKingdoms" };
            Rome2Remastered = new GameInformation() { Type = GameTypeEnum.Rome_2_Remastered, DisplayName = "Rome II - Re", DefaultPackType = "PFH5", ShortID = "Rome2Remastered" };
            Attilla = new GameInformation() { Type = GameTypeEnum.Attila, DisplayName = "Attila", DefaultPackType = "PFH5", ShortID = "Attila" };
            Games = new List<GameInformation>() { Warhammer, Warhammer2, Troy, ThreeKingdoms, Rome2Remastered, Attilla };
        }

        public static GameInformation GetGameById(GameTypeEnum type)
        {
            return Games.FirstOrDefault(x => x.Type == type);
        }

    }
}
