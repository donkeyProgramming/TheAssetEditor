using System.Text;
using System.Text.RegularExpressions;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public interface IWsMaterialRepository
    {
        bool TryGetExistingMaterial(string wsMaterialContent, out string wsMaterialPath);
        void AddMaterial(string wsMaterialContent, string wsMaterialPath);
        bool CanWriteMaterial(string wsMaterialPath, string ownerWsModelPath);
    }

    class WsMaterialRepository : IWsMaterialRepository
    {
        private readonly IPackFileService _packFileService;
        private readonly Dictionary<string, string> _map = [];
        private readonly Dictionary<string, string> _contentByPath = [];
        private Dictionary<string, HashSet<string>>? _wsModelsByMaterialPath;
        private readonly ILogger _logger = Logging.Create<WsMaterialRepository>();

        public WsMaterialRepository(IPackFileService packFileService)
        {
            _packFileService = packFileService;
            LoadAllExistingMaterials();
        }

        public int ExistingMaterialsCount() => _map.Count;

        public string GetExistingOrAddMaterial(string wsMaterialContent, string wsMaterialPath, out bool isNew)
        {
            isNew = TryGetExistingMaterial(wsMaterialContent, out var existingPath) == false;
            if (isNew == false)
                return existingPath;

            AddMaterial(wsMaterialContent, wsMaterialPath);
            return wsMaterialPath;
        }

        public bool TryGetExistingMaterial(string wsMaterialContent, out string wsMaterialPath)
        {
            var found = _map.TryGetValue(SanatizeMaterial(wsMaterialContent), out var path);
            wsMaterialPath = path ?? string.Empty;
            return found;
        }

        public void AddMaterial(string wsMaterialContent, string wsMaterialPath) => Register(SanatizeMaterial(wsMaterialContent), wsMaterialPath);

        // Materials are shared (dedup, mesh-based names), so only overwrite one nothing else can be using
        public bool CanWriteMaterial(string wsMaterialPath, string ownerWsModelPath)
        {
            var editablePack = _packFileService.GetEditablePack();
            foreach (var container in _packFileService.GetAllPackfileContainers())
            {
                if (container != editablePack && container.ContainsFile(wsMaterialPath))
                    return false;
            }

            if (editablePack == null || editablePack.ContainsFile(wsMaterialPath) == false)
                return true;

            _wsModelsByMaterialPath ??= LoadWsModelReferences(editablePack);
            if (_wsModelsByMaterialPath.TryGetValue(PathNormalization.NormalizeFileName(wsMaterialPath), out var users) == false)
                return true;

            var owner = PathNormalization.NormalizeFileName(ownerWsModelPath);
            return users.All(x => x == owner);
        }

        void Register(string sanitizedWsMaterial, string wsMaterialPath)
        {
            // The path no longer holds its old content, so stop offering it for that content
            var pathKey = PathNormalization.NormalizeFileName(wsMaterialPath);
            if (_contentByPath.TryGetValue(pathKey, out var oldContent) && _map.TryGetValue(oldContent, out var oldPath) && PathNormalization.NormalizeFileName(oldPath) == pathKey)
                _map.Remove(oldContent);

            _map[sanitizedWsMaterial] = wsMaterialPath;
            _contentByPath[pathKey] = sanitizedWsMaterial;
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

        void LoadAllExistingMaterials()
        {
            // Last loaded pack wins, as in FindFile: for a path, and for identical content at different paths
            var containers = _packFileService.GetAllPackfileContainers();
            var seenPaths = new HashSet<string>();
            for (var i = containers.Count - 1; i >= 0; i--)
            {
                var materialPacks = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".material", containers[i]);
                foreach (var (fileName, pack) in materialPacks.Where(x => x.Pack.Name.Contains(".xml.material")))
                {
                    if (seenPaths.Add(PathNormalization.NormalizeFileName(fileName)) == false)
                        continue;

                    try
                    {
                        var bytes = pack.DataSource.ReadData();
                        var content = SanatizeMaterial(Encoding.UTF8.GetString(bytes));
                        if (_map.ContainsKey(content) == false)
                            Register(content, fileName);
                    }
                    catch (Exception e)
                    {
                        _logger.Here().Error(e, "Failed to load material {MaterialPath} from pack {PackName}", fileName, pack.Name);
                    }
                }
            }
        }

        Dictionary<string, HashSet<string>> LoadWsModelReferences(IPackFileContainer pack)
        {
            var output = new Dictionary<string, HashSet<string>>();
            foreach (var (wsModelPath, wsModelFile) in PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".wsmodel", pack))
            {
                try
                {
                    var wsModel = new WsModelFile(wsModelFile);
                    foreach (var material in wsModel.MaterialList)
                    {
                        var materialPath = PathNormalization.NormalizeFileName(material.MaterialPath);
                        if (output.TryGetValue(materialPath, out var users) == false)
                            output[materialPath] = users = [];
                        users.Add(PathNormalization.NormalizeFileName(wsModelPath));
                    }
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Failed to read wsmodel {WsModelPath} while checking material references", wsModelPath);
                }
            }
            return output;
        }
    }

}
