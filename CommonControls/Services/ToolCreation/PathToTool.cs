using System.Text.RegularExpressions;
using CommonControls.Common;

namespace CommonControls.Services.ToolCreation
{
    public class PathToTool : IPackFileToToolSelector
    {
        string _extention;
        string _requiredPathSubString;

        public PathToTool(EditorEnums editorDisplayName, string extention, string requiredPathSubString)
        {
            _extention = extention;
            _requiredPathSubString = requiredPathSubString;
            EditorType = editorDisplayName;
        }

        public EditorEnums EditorType { get; private set; }

        public PackFileToToolSelectorResult CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (_extention == extention && fullPath.Contains(_requiredPathSubString))
                return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = true };

            return new PackFileToToolSelectorResult() { CanOpen = false, IsCoreTool = false };
        }
    }
}
