namespace GameWorld.Core.Services
{
   // public class MissingTextureResolver
//{
        /*
        public void ResolveMissingTextures(Rmv2MeshNode meshNode, PackFileService pfs)
        {
            var textureTypes = Enum.GetValues(typeof(TextureType)).Cast<TextureType>().ToList();

            var filePath = meshNode.OriginalFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var newPath = Path.ChangeExtension(filePath, ".wsmodel");
            var wsModelFile = pfs.FindFile(newPath);
            if (wsModelFile != null)
            {
                var wsModel = new WsModelFile(wsModelFile);
                var material = wsModel.MaterialList.FirstOrDefault(x => x.LodIndex == meshNode.LodIndex && x.PartIndex == meshNode.OriginalPartIndex);
                if (material != null)
                {
                    var wsMaterialFile = pfs.FindFile(material.MaterialPath);
                    if (wsMaterialFile != null)
                    {
                        var wsMaterialFileContent = new WsModelMaterialFile(wsMaterialFile);
                        foreach (var wsModelTexture in wsMaterialFileContent.Textures)
                            UpdateTextureIfMissing(meshNode, pfs, wsModelTexture.Key, wsModelTexture.Value);

                        meshNode.Material.AlphaMode = AlphaMode.Opaque;
                        if (wsMaterialFileContent.Alpha)
                            meshNode.Material.AlphaMode = AlphaMode.Transparent;
                    }
                }
            }
            else
            {
                // Ws model not found, try resolving using pattern
                var diffusePath = GetTexturePath(meshNode, TextureType.Diffuse);
                if (diffusePath == string.Empty)
                    return;

                var baseColourBasedOnDiffuse = diffusePath.Replace("diffuse.dds", "base_colour.dds", StringComparison.InvariantCultureIgnoreCase);
                var materialMapPathBasedOnDiffuse = diffusePath.Replace("diffuse.dds", "material_map.dds", StringComparison.InvariantCultureIgnoreCase);
                UpdateTextureIfMissing(meshNode, pfs, TextureType.BaseColour, baseColourBasedOnDiffuse);
                UpdateTextureIfMissing(meshNode, pfs, TextureType.MaterialMap, materialMapPathBasedOnDiffuse);


                var specularPath = GetTexturePath(meshNode, TextureType.Specular);
                if (specularPath == string.Empty)
                    return;

                var baseColourBasedOnSpec = specularPath.Replace("specular.dds", "base_colour.dds", StringComparison.InvariantCultureIgnoreCase);
                var materialMapPathBasedOnSpec = specularPath.Replace("specular.dds", "material_map.dds", StringComparison.InvariantCultureIgnoreCase);
                UpdateTextureIfMissing(meshNode, pfs, TextureType.BaseColour, baseColourBasedOnSpec);
                UpdateTextureIfMissing(meshNode, pfs, TextureType.MaterialMap, materialMapPathBasedOnSpec);
            }
        }*/

       /* string GetTexturePath(Rmv2MeshNode meshNode, TextureType texureType)
        {
            var texture = meshNode.Material.GetTexture(texureType);
            if (texture == null)
                return "";
            return texture.Value.Path;
        }*/
        /*
        void UpdateTextureIfMissing(Rmv2MeshNode meshNode, PackFileService pfs, TextureType texureType, string newPath)
        {
            var newFile = pfs.FindFile(newPath);
            if (newFile == null)
                return;

            var currentPath = GetTexturePath(meshNode, texureType);
            var currentFile = pfs.FindFile(currentPath);
            if (currentFile == null)
            {
                meshNode.UpdateTexture(newPath, texureType);
            }
        }*/
  //  }
}
