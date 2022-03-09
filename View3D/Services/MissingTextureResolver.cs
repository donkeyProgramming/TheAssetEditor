using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.Services;
using System;
using System.IO;
using System.Linq;
using View3D.SceneNodes;

namespace View3D.Services
{
    public class MissingTextureResolver
    {
        public void ResolveMissingTextures(Rmv2MeshNode meshNode, PackFileService pfs)
        {
            var textureTypes = Enum.GetValues(typeof(TexureType)).Cast<TexureType>().ToList();

            var filePath = meshNode.OriginalFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var newPath = Path.ChangeExtension(filePath, ".wsmodel");
            var wsModelFile = pfs.FindFile(newPath);
            if (wsModelFile != null)
            {
                var wsModel = new WsMaterial(wsModelFile);
                var material = wsModel.MaterialList.FirstOrDefault(x => x.LodIndex == 0 && x.PartIndex == meshNode.OriginalPartIndex);
                if (material != null)
                {
                    var wsMaterialFile = pfs.FindFile(material.Material);
                    if (wsMaterialFile != null)
                    {
                        var wsMaterialFileContent = new WsModelMaterialFile(wsMaterialFile, "");
                        foreach (var texture in textureTypes)
                        {
                            meshNode.UpdateTexture("", texture);
                            meshNode.UseTexture(texture, false);
                        }

                        foreach (var wsModelTexture in wsMaterialFileContent.Textures)
                        {
                            meshNode.UpdateTexture(wsModelTexture.Value, wsModelTexture.Key);
                            meshNode.UseTexture(wsModelTexture.Key, true);
                        }

                        meshNode.Material.AlphaMode = AlphaMode.Opaque;
                        if (wsMaterialFileContent.Alpha)
                            meshNode.Material.AlphaMode = AlphaMode.Transparent;
                    }
                }
            }
            else
            {
                // Ws model not found, try resolving using pattern
                var diffusePath = GetTexturePath(meshNode, TexureType.Diffuse);
                if (diffusePath == string.Empty)
                    return;

                var baseColourBasedOnDiffuse = diffusePath.Replace("diffuse.dds", "base_colour.dds", StringComparison.InvariantCultureIgnoreCase);
                var materialMapPathBasedOnDiffuse = diffusePath.Replace("diffuse.dds", "material_map.dds", StringComparison.InvariantCultureIgnoreCase);
                UpdateTextureIfMissing(meshNode, pfs, TexureType.BaseColour, baseColourBasedOnDiffuse);
                UpdateTextureIfMissing(meshNode, pfs, TexureType.MaterialMap, materialMapPathBasedOnDiffuse);

                var baseColourBasedOnSpec = diffusePath.Replace("specula.dds", "base_colour.dds", StringComparison.InvariantCultureIgnoreCase);
                var materialMapPathBasedOnSpec = diffusePath.Replace("specula.dds", "material_map.dds", StringComparison.InvariantCultureIgnoreCase);
                UpdateTextureIfMissing(meshNode, pfs, TexureType.BaseColour, baseColourBasedOnSpec);
                UpdateTextureIfMissing(meshNode, pfs, TexureType.MaterialMap, materialMapPathBasedOnSpec);
            }
        }

        string GetTexturePath(Rmv2MeshNode meshNode, TexureType texureType)
        {
            var texture = meshNode.Material.GetTexture(texureType);
            if (texture == null)
                return "";
            return texture.Value.Path;
        }


        void UpdateTextureIfMissing(Rmv2MeshNode meshNode, PackFileService pfs, TexureType texureType, string newPath)
        {
            var newFile = pfs.FindFile(newPath);
            if (newFile == null)
                return;

            var currentPath = GetTexturePath(meshNode, texureType);
            var currentFile = pfs.FindFile(currentPath);
            if (currentFile == null)
            {
                meshNode.UpdateTexture(newPath, texureType);
                meshNode.UseTexture(texureType, true);
            }


        }

    }
}
