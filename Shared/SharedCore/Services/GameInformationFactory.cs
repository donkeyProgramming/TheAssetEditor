namespace Shared.Core.Services
{
    public enum GameTypeEnum
    {
        Unknown = -1,
        Arena = 0,
        Attila,
        Empire,
        Napoleon,
        RomeRemastered,
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
        public GameInformation RomeRemastered { get; private set; }
        public GameInformation Attila { get; private set; }
        public GameInformation Pharaoh { get; private set; }
        public List<GameInformation> Games { get; private set; }

        public GameInformationFactory()
        {
            Warhammer = new GameInformation() { Type = GameTypeEnum.Warhammer1, DisplayName = "Total War: Warhammer", DefaultPackType = "PFH4", ShortID = "warhammer" };
            Warhammer2 = new GameInformation() { Type = GameTypeEnum.Warhammer2, DisplayName = "Total War: Warhammer II", DefaultPackType = "PFH5", ShortID = "warhammer2" };
            Warhammer3 = new GameInformation() { Type = GameTypeEnum.Warhammer3, DisplayName = "Total War: Warhammer III", DefaultPackType = "PFH5", ShortID = "warhammer3" };
            Troy = new GameInformation() { Type = GameTypeEnum.Troy, DisplayName = "Total War: Troy", DefaultPackType = "PFH5", ShortID = "troy" };
            ThreeKingdoms = new GameInformation() { Type = GameTypeEnum.ThreeKingdoms, DisplayName = "Total War: Three Kingdoms", DefaultPackType = "PFH5", ShortID = "ThreeKingdoms" };
            RomeRemastered = new GameInformation() { Type = GameTypeEnum.RomeRemastered, DisplayName = "Total War: Rome Remastered", DefaultPackType = "PFH5", ShortID = "RomeRemastered" };
            Attila = new GameInformation() { Type = GameTypeEnum.Attila, DisplayName = "Total War: Attila", DefaultPackType = "PFH5", ShortID = "Attila" };
            Pharaoh = new GameInformation() { Type = GameTypeEnum.Pharaoh, DisplayName = "Total War: Pharaoh", DefaultPackType = "PFH5", ShortID = "Pharaoh" };
            Games = [Warhammer, Warhammer2, Warhammer3, Troy, ThreeKingdoms, RomeRemastered, Attila, Pharaoh];
        }

        public GameInformation GetGameById(GameTypeEnum type)
        {
            return Games.First(x => x.Type == type);
        }
    }
}
