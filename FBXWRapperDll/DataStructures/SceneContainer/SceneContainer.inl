
extern "C" // not really needed but block looks nice/readable:)
{
    FBX_DLL extern void AllocateVertexWeights(wrapdll::SceneContainer* pInstance, int meshIndex, int weightCount)
    {
        pInstance->AllocateVertexWeights(meshIndex, weightCount);
    };

    FBX_DLL extern void AllocateMeshes(wrapdll::SceneContainer* pInstance, int meshCount)
    {
        pInstance->AllocateMeshes(meshCount);
    };

    FBX_DLL extern uint16_t* AllocateIndices(wrapdll::SceneContainer* pInstance, int meshIndex, int indexCount)
    {
        return pInstance->AllocateIndices(meshIndex, indexCount);
    };

    FBX_DLL extern BoneInfo* AllocateBones(wrapdll::SceneContainer* pInstance, int boneCount)
    {
        return pInstance->AllocateBones(boneCount);
    };

    FBX_DLL extern void SetIndices(wrapdll::SceneContainer* pInstance, int meshIndex, uint16_t* pIndices, int indexCount)
    {
        pInstance->SetIndices(meshIndex, pIndices, indexCount);
    };

    FBX_DLL extern  uint16_t* GetIndices(wrapdll::SceneContainer* pInstance, int meshindex, int* itemCount)
    {
        return pInstance->GetIndices(meshindex, itemCount);
    };

    FBX_DLL extern PackedCommonVertex* AllocateVertices(wrapdll::SceneContainer* pInstance, int meshIndex, int vertexCount)
    {
        return pInstance->AllocateVertices(meshIndex, vertexCount);
    };

    FBX_DLL extern void SetVertices(wrapdll::SceneContainer* pInstance, int meshIndex, PackedCommonVertex* pVertices, int vertexCount)
    {
        pInstance->SetVertices(meshIndex, pVertices, vertexCount);
    };

    FBX_DLL PackedCommonVertex* GetVertices(wrapdll::SceneContainer* pInstance, int meshindex, int* itemCount)
    {                
        auto ptr =  pInstance->GetVertices(meshindex, itemCount);
        return ptr;
    };  


    FBX_DLL extern void GetVertexWeights(wrapdll::SceneContainer* pInstance, int meshindex, VertexWeight** VertexWeight, int* itemCount)
    {
        pInstance->GetVertexWeights(meshindex, VertexWeight, itemCount);
    };    

    FBX_DLL extern char* GetMeshName(wrapdll::SceneContainer* pInstance, int meshindex)
    {
        return (char*)pInstance->GetMeshes()[meshindex].meshName.c_str();
    };

    FBX_DLL extern int GetMeshCount(wrapdll::SceneContainer* pInstance)
    {
        return static_cast<int>(pInstance->GetMeshes().size());
    };

    FBX_DLL extern char* GetSkeletonName(wrapdll::SceneContainer* pInstance)
    {
        return const_cast<char*>(pInstance->GetSkeletonName().c_str());
    };

    FBX_DLL extern void SetSkeletonName(wrapdll::SceneContainer* pInstance, const char* szSkeletonName)
    {
        pInstance->SetSkeletonName(szSkeletonName);
    };

    FBX_DLL
        extern FbxFileInfoData* GetFileInfo(wrapdll::SceneContainer* pInstance)
    {
        return &pInstance->GetFileInfo();
    };

   

}
