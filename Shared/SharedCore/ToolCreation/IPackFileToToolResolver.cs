using System.Text.RegularExpressions;

namespace Shared.Core.ToolCreation
{
    public interface IPackFileToToolResolver
    {
        PackFileToToolSelectorResult CanOpen(string fullPath);
        EditorEnums EditorType { get; }
    }

    public class PathToTool : IPackFileToToolResolver
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
                return new PackFileToToolSelectorResult() { CanOpen = true };

            return new PackFileToToolSelectorResult() { CanOpen = false };
        }
    }

    public class ExtensionToTool : IPackFileToToolResolver
    {
        readonly string[] _validExtentionsCore;
        readonly string[] _validExtentionsOptimal;

        public ExtensionToTool(EditorEnums editorDisplayName, string[] coreTools, string[] optionalTools = null)
        {
            _validExtentionsCore = coreTools;
            _validExtentionsOptimal = optionalTools;
            EditorType = editorDisplayName;
        }

        public EditorEnums EditorType { get; private set; }

        public PackFileToToolSelectorResult CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (extention.Contains("{") && extention.Contains("}"))
            {
                var ext2 = Regex.Match(extention, @"\..*\.(.*)\.(.*)");
                if (ext2.Success)
                {
                    extention = "." + ext2.Groups[1].Value + "." + ext2.Groups[2].Value;
                }
            }

            if (_validExtentionsCore != null)
            {
                foreach (var validExt in _validExtentionsCore)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true };
                }
            }

            if (_validExtentionsOptimal != null)
            {
                foreach (var validExt in _validExtentionsOptimal)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true,  };
                }
            }

            return new PackFileToToolSelectorResult() { CanOpen = false };
        }
    }
}
