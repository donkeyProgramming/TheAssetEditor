using Common.GameInformation;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common.ApplicationSettings
{
    public class ApplicationSettings
    {
        public class GamePathPair
        {
            public GameTypeEnum Game { get; set; }
            public string Path { get; set; }
        }

        public List<GamePathPair> GameDirectories { get; set; } = new List<GamePathPair>();
        public GameTypeEnum CurrentGame { get; set; } = GameTypeEnum.Warhammer2;
        public bool UseTextEditorForUnknownFiles{ get; set; } = true;
        public bool LoadCaPacksByDefault { get; set; } = true;
        public bool IsFirstTimeStartingApplication { get; set; } = true;
        public bool IsDeveloperRun { get; set; } = false;
    }
}
