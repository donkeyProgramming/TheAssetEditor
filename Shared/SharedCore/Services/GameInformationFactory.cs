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
        Warhammer,
        Warhammer2,
        Warhammer3,
        Troy,
        Pharaoh
    }

   //public enum PreferedShaderGroup
   //{ }
   //
   //public enum  PreferedRmvVersion
   //{
   //    
   //}
   //
   //public enum PreferedWsModelVersion
   //{ }
   //
   //public enum PreferedAnimationBinVersion
   //{ }
   //
    /*
     
     RegisterShader(PreferedShaderGroup.Wh3)
        .Shader("path", shaderEnum)
        .AddCapability<TCap, TWsModelSerializer, TRmvSerializer>();
     
     
     
     
     */

    public class GameInformation // Convert to record
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
        public GameInformation Rome_2 { get; private set; }
        public GameInformation Attila { get; private set; }
        public GameInformation Pharaoh { get; private set; }
        public List<GameInformation> Games { get; private set; }

        public GameInformationFactory()
        {
            Warhammer = new GameInformation() { Type = GameTypeEnum.Warhammer, DisplayName = "Warhammer", DefaultPackType = "PFH4", ShortID = "Warhammer" };
            Warhammer2 = new GameInformation() { Type = GameTypeEnum.Warhammer2, DisplayName = "Warhammer II", DefaultPackType = "PFH5", ShortID = "WarhammerII" };
            Warhammer3 = new GameInformation() { Type = GameTypeEnum.Warhammer3, DisplayName = "Warhammer III", DefaultPackType = "PFH5", ShortID = "WarhammerIII" };
            Troy = new GameInformation() { Type = GameTypeEnum.Troy, DisplayName = "Troy", DefaultPackType = "PFH5", ShortID = "troy" };
            ThreeKingdoms = new GameInformation() { Type = GameTypeEnum.ThreeKingdoms, DisplayName = "Three Kingdoms", DefaultPackType = "PFH5", ShortID = "ThreeKingdoms" };
            Rome_2 = new GameInformation() { Type = GameTypeEnum.Rome_2, DisplayName = "Rome II", DefaultPackType = "PFH5", ShortID = "Rome_2" };
            Attila = new GameInformation() { Type = GameTypeEnum.Attila, DisplayName = "Attila", DefaultPackType = "PFH5", ShortID = "Attila" };
            Pharaoh = new GameInformation() { Type = GameTypeEnum.Pharaoh, DisplayName = "Pharaoh", DefaultPackType = "PFH5", ShortID = "Pharaoh" };
            Games = [Warhammer, Warhammer2, Warhammer3, Troy, ThreeKingdoms, Rome_2, Attila, Pharaoh];
        }

        public GameInformation GetGameById(GameTypeEnum type)
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
                GameTypeEnum.Rome_2 => "Rome 2",
                GameTypeEnum.Shogun_2 => "Shogun 2",
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
