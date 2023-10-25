
extern "C" // not really needed but block looks nice/readable:)
{
    FBX_DLL_WRAP extern VertexWeight* AllocateVertexWeights(wrapdll::SceneContainer* pInstance, int meshIndex, int weightCount)
    {
        return pInstance->AllocateVertexWeights(meshIndex, weightCount);
    };

    FBX_DLL_WRAP extern void AllocateMeshes(wrapdll::SceneContainer* pInstance, int meshCount)
    {
        pInstance->AllocateMeshes(meshCount);
    };

    FBX_DLL_WRAP extern uint32_t* AllocateIndices(wrapdll::SceneContainer* pInstance, int meshIndex, int indexCount)
    {
        return pInstance->AllocateIndices(meshIndex, indexCount);
    };

    FBX_DLL_WRAP extern BoneInfo* AllocateBones(wrapdll::SceneContainer* pInstance, int boneCount)
    {
        return pInstance->AllocateBones(boneCount);
    };

    FBX_DLL_WRAP extern void SetIndices(wrapdll::SceneContainer* pInstance, int meshIndex, uint32_t* pIndices, int indexCount)
    {
        pInstance->SetIndices(meshIndex, pIndices, indexCount);
    };

    FBX_DLL_WRAP extern  uint32_t* GetIndices(wrapdll::SceneContainer* pInstance, int meshindex, int* itemCount)
    {
        return pInstance->GetIndices(meshindex, itemCount);
    };

    FBX_DLL_WRAP extern PackedCommonVertex* AllocateVertices(wrapdll::SceneContainer* pInstance, int meshIndex, int vertexCount)
    {
        return pInstance->AllocateVertices(meshIndex, vertexCount);
    };

    FBX_DLL_WRAP extern void SetVertices(wrapdll::SceneContainer* pInstance, int meshIndex, PackedCommonVertex* pVertices, int vertexCount)
    {
        pInstance->SetVertices(meshIndex, pVertices, vertexCount);
    };

    FBX_DLL_WRAP PackedCommonVertex* GetVertices(wrapdll::SceneContainer* pInstance, int meshindex, int* itemCount)
    {                
        auto ptr =  pInstance->GetVertices(meshindex, itemCount);
        return ptr;
    };  


    FBX_DLL_WRAP extern void GetVertexWeights(wrapdll::SceneContainer* pInstance, int meshindex, VertexWeight** VertexWeight, int* itemCount)
    {
        pInstance->GetVertexWeights(meshindex, VertexWeight, itemCount);
    };    

    FBX_DLL_WRAP extern char* GetMeshName(wrapdll::SceneContainer* pInstance, int meshindex)
    {
        return (char*)pInstance->GetMeshes()[meshindex].meshName.c_str();
    };

    FBX_DLL_WRAP extern int GetMeshCount(wrapdll::SceneContainer* pInstance)
    {
        return static_cast<int>(pInstance->GetMeshes().size());
    };

    FBX_DLL_WRAP extern char* GetSkeletonName(wrapdll::SceneContainer* pInstance)
    {
        return const_cast<char*>(pInstance->GetSkeletonName().c_str());
    };

    FBX_DLL_WRAP extern void SetSkeletonName(wrapdll::SceneContainer* pInstance, const char* szSkeletonName)
    {
        pInstance->SetSkeletonName(szSkeletonName);
    };

    FBX_DLL_WRAP extern void SetMeshName(wrapdll::SceneContainer* pInstance, int meshIndex, const char* szMeshName)
    {
        pInstance->GetMeshes() [meshIndex].meshName = szMeshName;        
    };

    FBX_DLL_WRAP
    extern FbxFileInfoData* GetFileInfo(wrapdll::SceneContainer* pInstance)
    {
        return &pInstance->GetFileInfo();
    };

   

}
