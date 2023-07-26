
extern "C" // not really needed but block looks nice/readable:)
{
	FBXWRAPPERDLL_API
		void GetPackedVertices(wrapdll::FBXSCeneContainer* pInstance, int meshindex, PackedCommonVertex** ppVertices, int* itemCount)
	{
		pInstance->GetVertices(meshindex, ppVertices, itemCount);
	};

	FBXWRAPPERDLL_API
		extern void GetIndices(wrapdll::FBXSCeneContainer* pInstance, int meshindex, uint16_t** ppVertices, int* itemCount)
	{
		pInstance->GetIndices(meshindex, ppVertices, itemCount);
	};

	FBXWRAPPERDLL_API
		extern void GetVertexWeights(wrapdll::FBXSCeneContainer* pInstance, int meshindex, VertexWeight** VertexWeight, int* itemCount)
	{
		pInstance->GetVertexWeights(meshindex, VertexWeight, itemCount);
	};

	FBXWRAPPERDLL_API
		extern char* GetMeshName(wrapdll::FBXSCeneContainer* pInstance, int meshindex)
	{
		return (char*)pInstance->GetMeshes()[meshindex].meshName.c_str();
	};

	FBXWRAPPERDLL_API
		extern int GetMeshCount(wrapdll::FBXSCeneContainer* pInstance)
	{
		return static_cast<int>(pInstance->GetMeshes().size());
	};

	FBXWRAPPERDLL_API
	extern char* GetSkeletonName(wrapdll::FBXSCeneContainer* pInstance, int meshindex)
	{
		return (char*)pInstance->GetSkeletonName().c_str();
	};

	FBXWRAPPERDLL_API
	extern FileInfoStruct* GetFileInfo(wrapdll::FBXSCeneContainer* pInstance)
	{
        return &pInstance->GetFileInfo();
	};

}
