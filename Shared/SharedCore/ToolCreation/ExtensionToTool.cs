using System.Text.RegularExpressions;

namespace Shared.Core.ToolCreation
{
    public class ExtensionToTool : IPackFileToToolSelector
    {
        string[] _validExtentionsCore;
        string[] _validExtentionsOptimal;

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
                //var index = extention.IndexOf("}");
                //extention = extention.Remove(0, index+1);
            }

            if (_validExtentionsCore != null)
            {
                foreach (var validExt in _validExtentionsCore)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = true };
                }
            }

            if (_validExtentionsOptimal != null)
            {
                foreach (var validExt in _validExtentionsOptimal)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = false };
                }
            }

            return new PackFileToToolSelectorResult() { CanOpen = false, IsCoreTool = false };
        }
    }
}
