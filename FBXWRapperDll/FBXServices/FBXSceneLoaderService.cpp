#include "FBXSceneLoaderService.h"

#include "..\Helpers\Geometry\FBXNodeSearcher.h"
#include "..\Processing\FBXMeshProcessor.h"
#include "..\Processing\FBXSkinProcessor.h"
#include "..\Processing\FBXMeshCreator.h"
#include "..\DLLDefines.h"

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
    ::strcpy_s<255>(m_sceneContainer.GetFileInfo().units, m_pFbxScene->GetGlobalSettings().GetSystemUnit().GetScaleFactorAsString_Plurial());
    m_sceneContainer.GetFileInfo().scaleFatorToMeters = static_cast<float>(m_pFbxScene->GetGlobalSettings().GetSystemUnit().GetConversionFactorTo(fbxsdk::FbxSystemUnit::m));
    m_sceneContainer.GetFileInfo().elementCount = m_pFbxScene->GetNodeCount();

    auto& destPackedMeshes = m_sceneContainer.GetMeshes();

    std::vector<fbxsdk::FbxMesh*> fbxMeshList;
    FBXNodeSearcher::FindMeshesInScene(m_pFbxScene, fbxMeshList);
    m_sceneContainer.GetSkeletonName() = FBXNodeSearcher::FetchSkeletonNameFromScene(m_pFbxScene);

    destPackedMeshes.clear();
    destPackedMeshes.resize(fbxMeshList.size());
    for (size_t meshIndex = 0; meshIndex < fbxMeshList.size(); meshIndex++)
    {
        std::vector<ControlPointInfluenceExt> vertexToControlPoint;
        FBXSkinProcessorService::ProcessSkin(fbxMeshList[meshIndex], destPackedMeshes[meshIndex], m_animFileBoneNames, vertexToControlPoint);

        FBXMeshCreator::MakeUnindexPackedMesh(m_pFbxScene, fbxMeshList[meshIndex], destPackedMeshes[meshIndex], vertexToControlPoint);

        log_action("Doing Tangents/Indexing");
        FbxMeshProcessor::doTangentAndIndexing(destPackedMeshes[meshIndex]);
        log_action("Done Tangents/Indexing");
    }

    return &m_sceneContainer;
}

#include "FbxSceneLoaderService.inl"