#include "FBXSceneLoaderService.h"

#include "..\Helpers\Geometry\FBXNodeSearcher.h"
#include "..\Processing\MeshProcessor.h"
#include "..\Processing\FBXSkinProcessor.h"
#include "..\Processing\FBXMeshCreator.h"
#include "..\DLLDefines.h"

wrapdll::FBXImporterService* wrapdll::FBXImporterService::CreateFromDiskFile(const std::string& path)
{
    auto pInstance = new wrapdll::FBXImporterService();
    pInstance->m_pSDKManager = FBXHelperFileUtil::InitializeSdkManager(); // for creation/cleanup
    pInstance->m_pFbxScene = FBXHelperFileUtil::InitScene(pInstance->m_pSDKManager, path, &pInstance->m_sceneContainer.GetFileInfo().sdkVersionUsed);
    strcpy_s<255>(pInstance->m_sceneContainer.GetFileInfo().fileName, path.c_str());

    if (!pInstance->m_pFbxScene)
    {
        LogActionError("Scene Creation Failed!!");
        return nullptr;
    }

    return pInstance;
}

wrapdll::FBXSCeneContainer* wrapdll::FBXImporterService::ProcessAndFillScene()
{      
    auto& destPackedMeshes = m_sceneContainer.GetMeshes();

    std::vector<fbxsdk::FbxMesh*> fbxMeshList;
    FBXNodeSearcher::FindMeshesInScene(m_pFbxScene, fbxMeshList);

    // TODO: check that "skeletonName" is only set once and in the proper place
    m_sceneContainer.GetSkeletonName() = FBXNodeSearcher::FetchSkeletonNameFromScene(m_pFbxScene);

    FillFileInfo();

    destPackedMeshes.clear();
    destPackedMeshes.resize(fbxMeshList.size());
    for (size_t meshIndex = 0; meshIndex < fbxMeshList.size(); meshIndex++)
    {
        std::vector<ControlPointInfluence> vertexToControlPoint;
        FBXSkinProcessorService::ProcessSkin(fbxMeshList[meshIndex], vertexToControlPoint);

        FBXMeshCreator::MakeUnindexPackedMesh(m_pFbxScene, fbxMeshList[meshIndex], destPackedMeshes[meshIndex], vertexToControlPoint);

        tools::SystemClock clock;        
        LogActionColor("Doing Tangents/Indexing");
        MeshProcessor::DoFinalMeshProcessing(destPackedMeshes[meshIndex]);        
        LogActionColor("Done Tangents/Indexing. Time: " + std::to_string(clock.GetLocalTime()) + " seconds.");
    }

    return &m_sceneContainer;
}

void wrapdll::FBXImporterService::FillFileInfo()
{
    auto& fileInfo = m_sceneContainer.GetFileInfo();
    
    strcpy_s<255>(fileInfo.units, m_pFbxScene->GetGlobalSettings().GetSystemUnit().GetScaleFactorAsString_Plurial());    
    strcpy_s<255>(fileInfo.skeletonName, m_sceneContainer.GetSkeletonName().c_str());    

    
	// use the recursive node search to get the count of differetn node types
    std::vector<fbxsdk::FbxNode*> fbxallNodes;
    FBXNodeSearcher::FindAllNodes(m_pFbxScene->GetRootNode(), fbxallNodes);
    
    std::vector<fbxsdk::FbxNode*> fbxMeshes;
    FBXNodeSearcher::FindFbxNodesByType(fbxsdk::FbxNodeAttribute::EType::eMesh, m_pFbxScene, fbxMeshes);

    std::vector<fbxsdk::FbxNode*> fbxBoneNodes;
    FBXNodeSearcher::FindFbxNodesByType(fbxsdk::FbxNodeAttribute::EType::eSkeleton, m_pFbxScene, fbxBoneNodes);
    
    fileInfo.elementCount = static_cast<int>(fbxMeshes.size());
    fileInfo.meshCount = static_cast<int>(fbxMeshes.size());
    fileInfo.boneCount = static_cast<int>(fbxBoneNodes.size());
    fileInfo.animationsCount = m_pFbxScene->GetSrcObjectCount<FbxAnimStack>();    

    // search for materaial for each mesh
    fileInfo.materialCount = 0;
    for (auto& mesh : fbxMeshes)
    {
        fileInfo.materialCount += mesh->GetMaterialCount();
    }    

    fileInfo.scaleFatorToMeters = static_cast<float>(m_pFbxScene->GetGlobalSettings().GetSystemUnit().GetConversionFactorTo(fbxsdk::FbxSystemUnit::m));
    fileInfo.elementCount = m_pFbxScene->GetNodeCount();
}

// DLL exported methods
#include "FbxSceneLoaderService.inl"

