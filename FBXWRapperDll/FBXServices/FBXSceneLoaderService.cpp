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
    pInstance->m_pFbxScene = FBXHelperFileUtil::InitScene(pInstance->m_pSDKManager, path, &pInstance->m_sceneContainer.GetFileInfo().sdkVersionUsed);
    strcpy_s<255>(pInstance->m_sceneContainer.GetFileInfo().fileName, path.c_str());

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

    // TODO: check that "skeletonName" is only set once and in the proper place
    m_sceneContainer.GetSkeletonName() = FBXNodeSearcher::FetchSkeletonNameFromScene(m_pFbxScene);

    FillFileInfo();

    destPackedMeshes.clear();
    destPackedMeshes.resize(fbxMeshList.size());
    for (size_t meshIndex = 0; meshIndex < fbxMeshList.size(); meshIndex++)
    {
        std::vector<ControlPointInfluenceExt> vertexToControlPoint;
        FBXSkinProcessorService::ProcessSkin(fbxMeshList[meshIndex], destPackedMeshes[meshIndex], m_animFileBoneNames, vertexToControlPoint);

        FBXMeshCreator::MakeUnindexPackedMesh(m_pFbxScene, fbxMeshList[meshIndex], destPackedMeshes[meshIndex], vertexToControlPoint);

        tools::SystemClock clock;
        log_action("Doing Tangents/Indexing");
        FbxMeshProcessor::doTangentAndIndexing(destPackedMeshes[meshIndex]);
        log_action("Done Tangents/Indexing. Time: " + std::to_string(clock.GetLocalTime()) );
    }

    return &m_sceneContainer;
}

void wrapdll::FBXImporterService::FillFileInfo()
{
    auto& fileInfo = m_sceneContainer.GetFileInfo();
    
    ::strcpy_s<255>(fileInfo.units, m_pFbxScene->GetGlobalSettings().GetSystemUnit().GetScaleFactorAsString_Plurial());    
    ::strcpy_s<255>(fileInfo.skeletonName, m_sceneContainer.GetSkeletonName().c_str());    

    std::vector<fbxsdk::FbxNode*> fbxMeshes;
    FBXNodeSearcher::FindFbxNodesByType(fbxsdk::FbxNodeAttribute::EType::eMesh, m_pFbxScene, fbxMeshes);
    
    fileInfo.meshCount = static_cast<int>(fbxMeshes.size());

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

