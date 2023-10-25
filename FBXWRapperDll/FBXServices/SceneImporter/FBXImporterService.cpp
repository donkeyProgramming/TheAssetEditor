#include "FBXImporterService.h"


#include "..\..\HelperUtils\Geometry\FBXNodeSearcher.h"
#include "..\..\HelperUtils\FBXHelperFileUtil.h"
#include "..\..\Processing\MeshProcessor.h"
#include "..\..\Processing\FBXSkinProcessor.h"
#include "..\..\Processing\PackedMeshCreator.h"
// TODO: fully remove?
//#include "..\..\DLLDefines.h"

wrapdll::FBXImporterService* wrapdll::FBXImporterService::CreateFromDiskFile(const std::string& path)
{
    auto pInstance = new wrapdll::FBXImporterService();
    pInstance->m_pSDKManager = FBXHelperFileUtil::InitializeSdkManager(); // for creation/cleanup
    pInstance->m_pFbxScene = FBXHelperFileUtil::InitSceneFromFile(pInstance->m_pSDKManager, path, &pInstance->m_sceneContainer.GetFileInfo().sdkVersionUsed);
    CopyToFixedString(pInstance->m_sceneContainer.GetFileInfo().fileName, path);

    if (!pInstance->m_pFbxScene)
    {
        LogActionError("Scene Creation Failed!!");
        return nullptr;
    }

    return pInstance;
}

wrapdll::SceneContainer* wrapdll::FBXImporterService::ProcessAndFillScene()
{      
    tools::SystemClock processingTimerClock;

    auto& destPackedMeshes = m_sceneContainer.GetMeshes();

    std::vector<fbxsdk::FbxMesh*> fbxMeshList;
    FBXNodeSearcher::FindMeshesInScene(m_pFbxScene, fbxMeshList);
    
    m_sceneContainer.GetSkeletonName() = FBXNodeSearcher::FetchSkeletonNameFromScene(m_pFbxScene, &m_sceneContainer.GetFileInfo().isIdStringBone);

    destPackedMeshes.clear();
    destPackedMeshes.resize(fbxMeshList.size());
    for (size_t meshIndex = 0; meshIndex < fbxMeshList.size(); meshIndex++)
    {
        std::vector<ControlPointInfluence> vertexToControlPoint;
        FBXSkinProcessorService::ProcessSkin(fbxMeshList[meshIndex], vertexToControlPoint);

        PackedMeshCreator::MakeUnindexedPackedMesh(m_pFbxScene, fbxMeshList[meshIndex], destPackedMeshes[meshIndex], vertexToControlPoint);

        tools::SystemClock tangentsClock;        
        LogActionColor("Doing Tangents/Indexing");
        MeshProcessor::DoFinalMeshProcessing(destPackedMeshes[meshIndex]);        
        LogActionColor("Done Tangents/Indexing. Time: " + std::to_string(tangentsClock.GetLocalTime()) + " seconds.");
    }

    LogActionColor("Scene Loading Done. ");
    ImplLog::LogAction_success("Processing Time : " + std::to_string(processingTimerClock.GetLocalTime()) + " seconds.");

    FillFileInfo();

    return &m_sceneContainer;
}

void wrapdll::FBXImporterService::FillFileInfo()
{
    auto& fileInfo = m_sceneContainer.GetFileInfo(); 
        
    CopyToFixedString(fileInfo.units, m_pFbxScene->GetGlobalSettings().GetSystemUnit().GetScaleFactorAsString_Plurial());    
    CopyToFixedString(fileInfo.skeletonName, m_sceneContainer.GetSkeletonName().c_str());

	// use the recursive node search to get the count of different node types
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
 
    // set "is there any weighting(deformation) data" flag
    // TODO: maybe to this more elegantly, during processing, maybe even test if the derformation data is valid
    VertexWeight* pVertexWeights = nullptr;
    int vertexWeightCount = 0; 
    m_sceneContainer.GetVertexWeights(0, &pVertexWeights, &vertexWeightCount);
    fileInfo.containsDerformationData = vertexWeightCount != 0;
}

// DLL exported methods
#include "FBXImporterService.inl"

