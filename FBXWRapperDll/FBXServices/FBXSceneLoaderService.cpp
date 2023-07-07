#include "FBXSceneLoaderService.h"

#include "..\Helpers\Geometry\FBXNodeSearcher.h"
#include "..\Processing\FBXMeshProcessor.h"
#include "..\Processing\FBXSkinProcessor.h"
#include "..\Processing\FBXMeshCreator.h"


wrapdll::FBXImporterService* wrapdll::FBXImporterService::CreateFromDiskFile(const std::string& path)
{
	auto pInstance = new wrapdll::FBXImporterService();
	pInstance->m_pSDKManager = FBXHelperFileUtil::InitializeSdkManager(); // for creation/cleanup
	pInstance->m_pFbxScene = FBXHelperFileUtil::InitScene(pInstance->m_pSDKManager, path);

	if (!pInstance->m_pFbxScene)
	{
		log_action_error("Scene Creation Failed!!");
		return nullptr;
	}

	return pInstance;
}

wrapdll::FBXSCeneContainer* wrapdll::FBXImporterService::ProcessAndFillScene()
{
	auto& destPackedMeshes = m_sceneContainer.GetMeshes();

	std::vector<fbxsdk::FbxMesh*> fbxMeshList;
	FBXNodeSearcher::FindMeshesInScene(m_pFbxScene, fbxMeshList);	

	destPackedMeshes.clear();
	destPackedMeshes.resize(fbxMeshList.size());
	for (size_t meshIndex = 0; meshIndex < fbxMeshList.size(); meshIndex++)
	{
		std::vector<ControlPointInfluences> vertexToControlPoint;
		FBXSkinProcessorService::ProcessSkin(fbxMeshList[meshIndex], destPackedMeshes[meshIndex], m_animFileBoneNames, vertexToControlPoint);
		FBXMeshCreator::MakeUnindexPackedMesh(m_pFbxScene, fbxMeshList[meshIndex], destPackedMeshes[meshIndex], vertexToControlPoint);

		log_action("Doing Tangents/Indexing");
		FbxMeshProcessor::doTangentAndIndexing(destPackedMeshes[meshIndex]);
		log_action("Done Tangents/Indexing");
	}	

	return &m_sceneContainer;
}

extern "C"
{
	FBXWRAPPERDLL_API_EXT
	wrapdll::FBXImporterService* CreateSceneFBX(char* path)	{
		
		return wrapdll::FBXImporterService::CreateFromDiskFile(path);
	};

	FBXWRAPPERDLL_API_EXT
		wrapdll::FBXSCeneContainer* ProcessAndFillScene(wrapdll::FBXImporterService* pInstance)
	{
		pInstance->ProcessAndFillScene();

		return &pInstance->GetSceneContainer();
	};

	FBXWRAPPERDLL_API_EXT
		char* GetSkeletonNameFromScene(wrapdll::FBXImporterService* pInstance, int meshindex)
	{
		return (char*)pInstance->GetSkeletonNameFromSceneNodes();
	};

	FBXWRAPPERDLL_API_EXT
		void AddBoneName(wrapdll::FBXImporterService* pInstance, char* boneName, int len)
	{
		std::string tempBoneName(boneName, len);
		pInstance->GetBoneNames().push_back(tempBoneName);
	};

	FBXWRAPPERDLL_API_EXT
		void ClearBoneNames(wrapdll::FBXImporterService* pInstance)
	{
		pInstance->GetBoneNames().clear();
	};

	FBXWRAPPERDLL_API_EXT
	void DeleteBaseObj(wrapdll::BaseInteropObject* pInstance)
	{
		if (pInstance != nullptr)
		{
			delete pInstance;
			pInstance = nullptr;
		}
	};
}