using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Shared.Core.PackFiles;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public interface IWsMaterialRepository
    {
        string GetExistingOrAddMaterial(string wsMaterialContent, string wsMaterialPath, out bool isNew);
    }

    class WsMaterialRepository : IWsMaterialRepository
    {
        private readonly Dictionary<string, string> _map;

        public WsMaterialRepository(IPackFileService packFileService)
        {
            _map = LoadAllExistingMaterials(packFileService);
        }

        public int ExistingMaterialsCount() => _map.Count;

        public string GetExistingOrAddMaterial(string wsMaterialContent, string wsMaterialPath, out bool isNew)
        {
            var sanitizedWsMaterial = SanatizeMaterial(wsMaterialContent);
            var found = _map.TryGetValue(sanitizedWsMaterial, out var path);
            if (found == false)
            {
                _map[sanitizedWsMaterial] = wsMaterialPath;
                isNew = true;
                return wsMaterialPath;
            }
            isNew = false;
            return path!;
        }

        string SanatizeMaterial(string wsMaterialContent)
        {
            var start = wsMaterialContent.IndexOf("<name>", System.StringComparison.InvariantCultureIgnoreCase);
            if (start != -1)
            {
                var end = wsMaterialContent.IndexOf("</name>", start, System.StringComparison.InvariantCultureIgnoreCase);
                var contentWithoutName = wsMaterialContent.Remove(start, end - start + "</name>".Length).ToLower();
                var contentWithoutNameAndWhiteSpace = ReplaceWhitespace(contentWithoutName, string.Empty);
                var finalStr = contentWithoutNameAndWhiteSpace.ToLower();

                return finalStr;
            }

            // a few wsmodels are very strange, just ignore them.
            // The result will just be more generated wsmodels. 
            // Not a huge issue.
            return wsMaterialContent;
        }

        private static readonly Regex sWhitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }

        Dictionary<string, string> LoadAllExistingMaterials(IPackFileService packFileService)
        {
            var materialList = new Dictionary<string, string>();

            var materialPacks = PackFileServiceUtility.FindAllWithExtentionIncludePaths(packFileService, ".material");
            materialPacks = materialPacks.Where(x => x.Pack.Name.Contains(".xml.material")).ToList();

            foreach (var (fileName, pack) in materialPacks)
            {
                var bytes = pack.DataSource.ReadData();
                var content = Encoding.UTF8.GetString(bytes);
                var sanitizedWsMaterial = SanatizeMaterial(content);

                materialList[sanitizedWsMaterial] = fileName;
            }

            return materialList;
        }
    }

}
