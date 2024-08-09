using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared.Core.PackFiles;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class WsMaterialRepository
    {
        private readonly Dictionary<string, string> _map;

        public WsMaterialRepository(PackFileService packFileService)
        {
            _map = LoadAllExistingMaterials(packFileService);
        }

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
            var start = wsMaterialContent.IndexOf("<name>");
            var end = wsMaterialContent.IndexOf("</name>", start);
            var contentWithoutName = wsMaterialContent.Remove(start, end).ToLower();

            return contentWithoutName;
        }

        Dictionary<string, string> LoadAllExistingMaterials(PackFileService packFileService)
        {
            var materialList = new Dictionary<string, string>();

            var materialPacks = packFileService.FindAllWithExtentionIncludePaths(".material");
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
