namespace Shared.Core.Settings
{
    public enum GameTypeEnum
    {
        Unknown = -1,
        Arena = 0,
        Attila,
        Empire,
        Napoleon,
        RomeRemastered,
        Rome2,
        Shogun2,
        ThreeKingdoms,
        ThronesOfBritannia,
        Warhammer,
        Warhammer2,
        Warhammer3,
        Troy,
        Pharaoh
    }

    public enum GameBnkVersion : uint
    { 
        Unknown = 0,
        Warhammer3 = 2147483784,
        Attila = 112
    }

    public enum WsModelVersion
    { 
        Version1,   // ThreeKingdoms, Troy, Warhammer2
        Version2,   // Troy
        Version3,   // Warhammer3
    }

    //RmvVersionEnum

    public class GameInformation(GameTypeEnum gameType, string displayName, string defaultPackType, uint? bankGeneratorVersion)
    {
        public GameTypeEnum Type { get; } = gameType;
        public string DisplayName { get; } = displayName;
        public string DefaultPackType { get; } = defaultPackType;

        public uint? BankGeneratorVersion { get; } = bankGeneratorVersion;
    }

    public static class GameInformationDatabase
    {
        static public List<GameInformation> Games { get; private set; }    // Convert to dictionary

        static GameInformationDatabase()
        {
            var Warhammer = new GameInformation(GameTypeEnum.Warhammer,  "Warhammer",  "PFH4",  null );
            var Warhammer2 = new GameInformation(GameTypeEnum.Warhammer2,  "Warhammer II",  "PFH5", null );
            var Warhammer3 = new GameInformation(GameTypeEnum.Warhammer3, "Warhammer III", "PFH5", 2147483784);
            var Troy = new GameInformation(GameTypeEnum.Troy, "Troy",  "PFH5",  null );
            var ThreeKingdoms = new GameInformation( GameTypeEnum.ThreeKingdoms,  "Three Kingdoms",  "PFH5",  null );
            var Rome2 = new GameInformation(GameTypeEnum.Rome2, "Rome II", "PFH5", null );
            var Attila = new GameInformation(GameTypeEnum.Attila,  "Attila",  "PFH5",  112 );
            var Pharaoh = new GameInformation(GameTypeEnum.Pharaoh,  "Pharaoh",  "PFH5",  null );

            Games = [Warhammer, Warhammer2, Warhammer3, Troy, ThreeKingdoms, Rome2, Attila, Pharaoh];
        }

        public static GameInformation GetGameById(GameTypeEnum type)
        {
            return Games.First(x => x.Type == type);
        }

        public static string GetEnumAsString(GameTypeEnum game)
        {
            return game switch
            {
                GameTypeEnum.Unknown => "Unknown",
                GameTypeEnum.Arena => "Arena",
                GameTypeEnum.Attila => "Attila",
                GameTypeEnum.Empire => "Empire",
                GameTypeEnum.Napoleon => "Napoleon",
                GameTypeEnum.RomeRemastered => "Rome Remastered",
                GameTypeEnum.Rome2 => "Rome II",
                GameTypeEnum.Shogun2 => "Shogun 2",
                GameTypeEnum.ThreeKingdoms => "Three Kingdoms",
                GameTypeEnum.ThronesOfBritannia => "Thrones of Britannia",
                GameTypeEnum.Warhammer => "Warhammer",
                GameTypeEnum.Warhammer2 => "Warhammer II",
                GameTypeEnum.Warhammer3 => "Warhammer III",
                GameTypeEnum.Troy => "Troy",
                GameTypeEnum.Pharaoh => "Pharaoh",
                _ => throw new Exception($"Unknown game - {game}"),
            };
        }
    }
}
