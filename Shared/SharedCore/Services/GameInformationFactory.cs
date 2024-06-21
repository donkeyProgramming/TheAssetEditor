namespace Shared.Core.Services
{
    public enum GameTypeEnum
    {
        Unknown = -1,
        Arena = 0,
        Attila,
        Empire,
        Napoleon,
        Rome_2_Remastered,
        Rome_2,
        Shogun_2,
        ThreeKingdoms,
        ThronesOfBritannia,
        Warhammer1,
        Warhammer2,
        Warhammer3,
        Troy,
        Pharaoh
    }

    public class GameInformation
    {
        public GameTypeEnum Type { get; set; }
        public string DisplayName { get; set; }
        public string DefaultPackType { get; set; }
        public string ShortID { get; set; }
    }

    public class GameInformationFactory
    {
        public GameInformation Warhammer { get; private set; }
        public GameInformation Warhammer2 { get; private set; }
        public GameInformation Warhammer3 { get; private set; }
        public GameInformation Troy { get; private set; }
        public GameInformation ThreeKingdoms { get; private set; }
        public GameInformation Rome2Remastered { get; private set; }
        public GameInformation Attilla { get; private set; }
        public GameInformation Pharaoh { get; private set; }
        public List<GameInformation> Games { get; private set; }

        public GameInformationFactory()
        {
            Warhammer = new GameInformation() { Type = GameTypeEnum.Warhammer1, DisplayName = "Warhammer", DefaultPackType = "PFH4", ShortID = "warhammer" };
            Warhammer2 = new GameInformation() { Type = GameTypeEnum.Warhammer2, DisplayName = "Warhammer II", DefaultPackType = "PFH5", ShortID = "warhammer2" };
            Warhammer3 = new GameInformation() { Type = GameTypeEnum.Warhammer3, DisplayName = "Warhammer III", DefaultPackType = "PFH5", ShortID = "warhammer3" };
            Troy = new GameInformation() { Type = GameTypeEnum.Troy, DisplayName = "Troy", DefaultPackType = "PFH5", ShortID = "troy" };
            ThreeKingdoms = new GameInformation() { Type = GameTypeEnum.ThreeKingdoms, DisplayName = "Three Kingdoms", DefaultPackType = "PFH5", ShortID = "ThreeKingdoms" };
            Rome2Remastered = new GameInformation() { Type = GameTypeEnum.Rome_2_Remastered, DisplayName = "Rome II - Re", DefaultPackType = "PFH5", ShortID = "Rome2Remastered" };
            Attilla = new GameInformation() { Type = GameTypeEnum.Attila, DisplayName = "Attila", DefaultPackType = "PFH5", ShortID = "Attila" };
            Pharaoh = new GameInformation() { Type = GameTypeEnum.Pharaoh, DisplayName = "Pharaoh", DefaultPackType = "PFH%", ShortID = "Pharaoh" };
            Games = new List<GameInformation>() { Warhammer, Warhammer2, Warhammer3, Troy, ThreeKingdoms, Rome2Remastered, Attilla, Pharaoh };
        }

        public GameInformation GetGameById(GameTypeEnum type)
        {
            return Games.First(x => x.Type == type);
        }
    }
}
