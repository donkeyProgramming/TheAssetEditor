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
        Unsupported = 0,
        Warhammer3 = 2147483784,
        Attila = 112
    }

    public enum PackFileVersion
    {
        PFH0,
        PFH2,
        PFH3,
        PFH4,
        PFH5,
        PFH6
    }

    public enum WsModelVersion
    {
        Unknown = 0,
        Version1,
        Version2,
        Version3,
    }

    //RmvVersionEnum

    public class GameInformation(GameTypeEnum gameType, string displayName, PackFileVersion packFileVersion, GameBnkVersion bankGeneratorVersion, WsModelVersion wsModelVersion)
    {
        public GameTypeEnum Type { get; } = gameType;
        public string DisplayName { get; } = displayName;
        public PackFileVersion PackFileVersion { get; } = packFileVersion;
        public GameBnkVersion BankGeneratorVersion { get; } = bankGeneratorVersion;
        public WsModelVersion WsModelVersion { get; } = wsModelVersion;
    }

    public static class GameInformationDatabase
    {
        static public List<GameInformation> Games { get; private set; }    // Convert to dictionary

        static GameInformationDatabase()
        {
            var warhammer = new GameInformation(GameTypeEnum.Warhammer, "Warhammer", PackFileVersion.PFH4, GameBnkVersion.Unsupported, WsModelVersion.Unknown );
            var warhammer2 = new GameInformation(GameTypeEnum.Warhammer2, "Warhammer II", PackFileVersion.PFH5, GameBnkVersion.Unsupported, WsModelVersion.Version1 );
            var warhammer3 = new GameInformation(GameTypeEnum.Warhammer3, "Warhammer III", PackFileVersion.PFH5, GameBnkVersion.Warhammer3, WsModelVersion.Version3);
            var troy = new GameInformation(GameTypeEnum.Troy, "Troy", PackFileVersion.PFH5, GameBnkVersion.Unsupported, WsModelVersion.Unknown);
            var threeKingdoms = new GameInformation( GameTypeEnum.ThreeKingdoms, "Three Kingdoms", PackFileVersion.PFH5, GameBnkVersion.Unsupported, WsModelVersion.Version1 );
            var rome2 = new GameInformation(GameTypeEnum.Rome2, "Rome II", PackFileVersion.PFH5, GameBnkVersion.Unsupported, WsModelVersion.Unknown);
            var attila = new GameInformation(GameTypeEnum.Attila, "Attila", PackFileVersion.PFH5, GameBnkVersion.Attila, WsModelVersion.Unknown);
            var pharaoh = new GameInformation(GameTypeEnum.Pharaoh, "Pharaoh", PackFileVersion.PFH5, GameBnkVersion.Unsupported, WsModelVersion.Unknown);

            Games = [warhammer, warhammer2, warhammer3, troy, threeKingdoms, rome2, attila, pharaoh];
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
