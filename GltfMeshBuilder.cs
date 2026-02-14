public List<IMeshBuilder<MaterialBuilder>> Build(RmvFile rmv2, List<TextureResult> textures, RmvToGltfExporterSettings settings, bool willUseSkeleton = false)
{
    var lodLevel = rmv2.ModelList.First();
    var hasSkeleton = string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;

    var meshes = new List<IMeshBuilder<MaterialBuilder>>();
    for(var i = 0; i < lodLevel.Length; i++)
    {
        var rmvMesh = lodLevel[i];
        var meshTextures = textures.Where(x=>x.MeshIndex == i).ToList();
        var gltfMaterial = Create(settings, rmvMesh.Material.ModelName + "_Material", meshTextures);
        var gltfMesh = GenerateMesh(rmvMesh.Mesh, rmvMesh.Material.ModelName, gltfMaterial, hasSkeleton && willUseSkeleton, settings.MirrorMesh);
        meshes.Add(gltfMesh);
    }
    return meshes;
}
