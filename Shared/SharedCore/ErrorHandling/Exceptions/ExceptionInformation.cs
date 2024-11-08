using Shared.Core.Services;

namespace Shared.Core.ErrorHandling.Exceptions
{
    public class ExceptionInformation
    {
        // Info about loaded packfile
        public record PackFileContainerInfo(bool IsMainEditable, bool IsCa, string Name, string SystemPath);
       


        public List<PackFileContainerInfo> ActivePackFiles { get; set; } = [];

        // General info
        public GameTypeEnum CurrentGame { get; set; }

        public ApplicationSettings? Settings { get; set; }
        public uint NumberOfOpenEditors { get; set; }
        public uint NumberOfOpenedEditors { get; set; }
        public double RunTimeInSeconds { get; set; }
        public string AssetEditorVersion { get; set; } = "Not set";

        // Exception Info
        public string[] ExceptionMessage { get; set; } = [];
        public string? StackTrace { get; set; }
        public string UserComment { get; set; } = "";

        // Contex info
        public string? CurrentEditorName { get; set; }
        public string EditorInputFile { get; set; } = "Not set";
        public string EditorInputFileFull { get; set; } = "Not set";
        public string EditorInputFilePack { get; set; } = "Not set";
        public List<string> LogHistory { get; set; } = [];

        // System info
        public string Culture { get; internal set; } = "Not set";
        public string OSVersion { get; internal set; } = "Not set";
    }
}
