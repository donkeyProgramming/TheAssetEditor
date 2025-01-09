namespace Shared.Core.Settings
{
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
     

     
     
     
            Warhammer3 = GameInformationBuilder
                .Create(GameTypeEnum.Warhammer3, "Warhammer III")
                .BankGeneratorVersion(2147483784)
                .WsModelVersion(5)
                .PreferedRmvVersion(7)
                .ShaderVersion(213)
                .AnimationBinVersion(11)
                .TwuiVersion(142)
                .Build();
     
     */


    public class GameInformationBuilder()
    {
        private GameInformation _instance;

        public static GameInformationBuilder Create(GameTypeEnum type, string displayName)
        {
            return new GameInformationBuilder()
            {
                _instance = new GameInformation(type, displayName, Settings.PackFileVersion.PFH5, Settings.GameBnkVersion.Unsupported, Settings.WsModelVersion.Unknown)
            };
        }

        public GameInformationBuilder PackFileVersion(uint version)
        {
            return this;
        }

        public GameInformationBuilder ShaderVersion(int version)
        {
            return this;
        }

        public GameInformationBuilder WsModelVersion(int version)
        {
            return this;
        }

        public GameInformationBuilder TwuiVersion(int version)
        {
            return this;
        }

        public GameInformationBuilder PreferedRmvVersion(int version)
        {
            return this;
        }

        public GameInformationBuilder GameBnkVersion(uint version)
        {
            return this;
        }

        public GameInformationBuilder AnimationBinVersion(uint version)
        {
            return this;
        }

        public GameInformation Build() => _instance;
    }
}
