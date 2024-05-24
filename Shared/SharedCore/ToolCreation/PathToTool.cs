using System.Text.RegularExpressions;

namespace Shared.Core.ToolCreation
{
    public class PathToTool : IPackFileToToolSelector
    {
        private readonly string _extension;
        private readonly string _requiredPathSubString;

        public PathToTool(EditorEnums editorDisplayName, string extension, string requiredPathSubString)
        {
            _extension = extension;
            _requiredPathSubString = requiredPathSubString;
            EditorType = editorDisplayName;
        }

        public EditorEnums EditorType { get; private set; }

        public PackFileToToolSelectorResult CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (_extension == extention && fullPath.Contains(_requiredPathSubString))
                return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = true };

            return new PackFileToToolSelectorResult() { CanOpen = false, IsCoreTool = false };
        }
    }
}
