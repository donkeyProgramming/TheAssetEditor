#include "FBXMeshSkinCreator.h"
#include "FBXMeshSkinCreator.h"

bool wrapdll::FBXMeshSkinCreator::CreateSkin(fbxsdk::FbxScene* poFbxScene, fbxsdk::FbxNode* pFbxNodeMesh, const PackedMesh& inMesh, SceneContainer& sceneContainer)
{
    using namespace fbxsdk;

    auto timedLogger = TimeLogAction::PrintStart("Making Skinning for mesh: " + inMesh.meshName);

    if (!ErrorChecking::CheckParamsAndLog(poFbxScene, pFbxNodeMesh))
    {
        return false;
    }

    auto& boneNodes = sceneContainer.GetFbxBoneNodes();
    auto& bones = sceneContainer.GetBones();
    auto poMesh = pFbxNodeMesh->GetMesh();

    std::vector<fbxsdk::FbxCluster*> m_meshFbxClusters;
    InitClusters(poFbxScene, inMesh, bones, boneNodes);

    // run trough all vertices
    AddWeghts(inMesh, bones);

    // asign global matrices to clusters (cluster = bone)
    AssignGeomtry(pFbxNodeMesh, boneNodes);

    auto pSkin = fbxsdk::FbxSkin::Create(poFbxScene, ("Skin--" + inMesh.meshName).c_str());
    FillFbxSkin(pSkin, inMesh);

    fbxsdk::FbxGeometry* lPatchAttribute = (fbxsdk::FbxGeometry*)pFbxNodeMesh->GetNodeAttribute();

    // add this set of influences to that mesh
    auto indexOfAddedDeformer = poMesh->AddDeformer(pSkin);

    // TODO: remove?
    //CheckSkinWeights(poMesh, pSkin);
    auto DEBUG_controlPointCount = poMesh->GetControlPointsCount();

    timedLogger.PrintDone();

    return true;
}

void wrapdll::FBXMeshSkinCreator::CheckSkinWeights(fbxsdk::FbxMesh* poMesh, fbxsdk::FbxSkin* pSkin)
{
    // check skin
    auto controlPointCount = poMesh->GetControlPointsCount();
    for (int testCtrlPointIndex = 0; testCtrlPointIndex < controlPointCount; testCtrlPointIndex++)
    {
        auto testWeight = 0.0;

        auto clusterCount = pSkin->GetClusterCount();
        for (int clusterIndex = 0; clusterIndex < clusterCount; clusterIndex++)
        {
            auto cluster = pSkin->GetCluster(clusterIndex);

            for (int controlPointIndex = 0; controlPointIndex < cluster->GetControlPointIndicesCount(); controlPointIndex++)
            {
                auto currentControlPoint = cluster->GetControlPointIndices()[controlPointIndex];
                auto weight = cluster->GetControlPointWeights()[controlPointIndex];


                if (testCtrlPointIndex == currentControlPoint)
                {
                    testWeight += weight;
                }

            }
        }

        if (testWeight < 0.95 || testWeight > 1.05)
        {
            LogActionError("Bad weight");
        }
    }

}

bool wrapdll::FBXMeshSkinCreator::FillFbxSkin(fbxsdk::FbxSkin* pSkin, const PackedMesh& inMesh)
{
    for (size_t clusterIndex = 0; clusterIndex < m_meshFbxClusters.size(); clusterIndex++)
    {
        auto bAddClusterResult = pSkin->AddCluster(m_meshFbxClusters[clusterIndex]);

        if (!bAddClusterResult)
        {
            return LogActionError("Adding cluster failed for mesh: " + inMesh.meshName);
        }
    }

    return true;
}

void wrapdll::FBXMeshSkinCreator::InitClusters(fbxsdk::FbxScene* poFbxScene, PackedMesh inMesh, const std::vector<BoneInfo>& bones, const std::vector<fbxsdk::FbxNode*>& boneNodes)
{
    m_meshFbxClusters.resize(bones.size());
    for (size_t clusterIndex = 0; clusterIndex < bones.size(); clusterIndex++)
    {
        m_meshFbxClusters[clusterIndex] = fbxsdk::FbxCluster::Create(poFbxScene, (inMesh.meshName + "__CLUSTER").c_str());
        m_meshFbxClusters[clusterIndex]->SetLink(boneNodes[clusterIndex]);

        m_meshFbxClusters[clusterIndex]->SetLinkMode(fbxsdk::FbxCluster::ELinkMode::eTotalOne);
    }
}

void wrapdll::FBXMeshSkinCreator::AssignGeomtry(fbxsdk::FbxNode* pMeshNode, std::vector<fbxsdk::FbxNode*>& boneNodes)
{
    FbxAMatrix lXMatrixStatic = pMeshNode->EvaluateGlobalTransform();
    auto DEBUG__EMPTY_CLUSTERS = 0; // TODO: remove
    for (size_t clusterIndex = 0; clusterIndex < m_meshFbxClusters.size(); clusterIndex++)
    {
        m_meshFbxClusters[clusterIndex]->SetTransformMatrix(lXMatrixStatic);

        FbxAMatrix lXMatrix = boneNodes[clusterIndex]->EvaluateGlobalTransform();
        m_meshFbxClusters[clusterIndex]->SetTransformLinkMatrix(lXMatrix);
    }
}

void wrapdll::FBXMeshSkinCreator::AddWeghts(const PackedMesh& inMesh, std::vector<BoneInfo>& bones)
{
    auto timedLoggerMsg = TimeLogAction::PrintStart("Adding weights to mesh: " + inMesh.meshName);

    std::map<std::string, int> boneNameMap;
    for (int boneIndex = 0; boneIndex < bones.size(); boneIndex++)
    {
        boneNameMap[tools::toLower(bones[boneIndex].name)] = bones[boneIndex].id;
    }

    for (size_t vertexWeightIndex = 0; vertexWeightIndex < inMesh.vertexWeights.size(); vertexWeightIndex++)
    {
        float weight = inMesh.vertexWeights[vertexWeightIndex].weight;
        int vertexIndex = inMesh.vertexWeights[vertexWeightIndex].vertexIndex;
        auto itBoneIndex = boneNameMap.find(tools::toLower(inMesh.vertexWeights[vertexWeightIndex].boneName));
        if (itBoneIndex == boneNameMap.end())
        {
            LogActionError("Bone not found: " + inMesh.vertexWeights[vertexWeightIndex].boneName);
            return;
        }

        m_meshFbxClusters[itBoneIndex->second]->AddControlPointIndex(vertexIndex, weight);
    }

    timedLoggerMsg.PrintDone();
}
