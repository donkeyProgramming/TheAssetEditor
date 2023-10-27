#include "..\..\DataStructures\SceneContainer\SceneContainer.h"
#include "..\..\HelperUtils\Tools.h"
#include "..\..\HelperUtils\Geometry\FBXSkinHelperUtil.h"
#include "..\..\ErrorCkecking\ErrorChecking.h"

#include <string> 
#include <fbxsdk.h>

#pragma once

namespace wrapdll
{

    class IFbxSkinCreator
    {
        static bool CreateFbxSkin(
            fbxsdk::FbxScene* poFbxScene,
            fbxsdk::FbxNode* pFbxNodeMesh,
            const PackedMesh& inMesh,
            SceneContainer& sceneContainer)
        {


        };
    };

    class FBXMeshSkinCreator
    {
    public:
        static bool AddAddSkinningToFbxMesh(
            fbxsdk::FbxScene* poFbxScene,
            fbxsdk::FbxNode* pFbxNodeMesh,
            const PackedMesh& inMesh,
            SceneContainer& sceneContainer)
        {
            using namespace fbxsdk;

            auto timedLogger = TimeLogAction::PrintStart("Making Skinning for mesh: " + inMesh.meshName);

            if (!ErrorChecking::CheckParamsAndLog(poFbxScene, pFbxNodeMesh))
            {
                return false;
            }

            auto& bones = sceneContainer.GetBones();
            auto& boneNodes = sceneContainer.GetFbxBoneNodes();
            auto poMesh = pFbxNodeMesh->GetMesh();

            std::vector<fbxsdk::FbxCluster*> meshClusters;
            InitClusters(meshClusters, bones, poFbxScene, inMesh, boneNodes);

            // run trough all vertices
            AddWeghts(inMesh, bones, meshClusters);

            // asign global matrices to clusters (cluster = bone)
            AssignGeomtry(pFbxNodeMesh, meshClusters, boneNodes);

            auto pSkin = fbxsdk::FbxSkin::Create(poFbxScene, ("Skin--" + inMesh.meshName).c_str());
            FillFbxSkin(meshClusters, pSkin, inMesh);

            fbxsdk::FbxGeometry* lPatchAttribute = (fbxsdk::FbxGeometry*)pFbxNodeMesh->GetNodeAttribute();

            // add this set of influences to that mesh
            auto indexOfAddedDeformer = poMesh->AddDeformer(pSkin);

            timedLogger.PrintDone();

            return true;
        }






        static bool FillFbxSkin(std::vector<fbxsdk::FbxCluster*>& meshClusters, fbxsdk::FbxSkin* pSkin, const PackedMesh& inMesh)
        {
            for (size_t clusterIndex = 0; clusterIndex < meshClusters.size(); clusterIndex++)
            {
                auto bAddClusterResult = pSkin->AddCluster(meshClusters[clusterIndex]);

                if (!bAddClusterResult)
                {
                    return LogActionError("Adding clust failed for mesh: " + inMesh.meshName);
                }
            }

            return true;
        }

        static void AssignGeomtry(fbxsdk::FbxNode* pMeshNode, std::vector<fbxsdk::FbxCluster*>& meshClusters, std::vector<fbxsdk::FbxNode*>& boneNodes)
        {

            FbxAMatrix lXMatrixStatic = pMeshNode->EvaluateGlobalTransform();
            auto DEBUG__EMPTY_CLUSTERS = 0; // TODO: remove
            for (size_t clusterIndex = 0; clusterIndex < meshClusters.size(); clusterIndex++)
            {
                meshClusters[clusterIndex]->SetTransformMatrix(lXMatrixStatic);

                FbxAMatrix lXMatrix = boneNodes[clusterIndex]->EvaluateGlobalTransform();
                meshClusters[clusterIndex]->SetTransformLinkMatrix(lXMatrix);
            }
        }

        static void AddWeghts(const PackedMesh& inMesh, std::vector<BoneInfo>& bones, std::vector<fbxsdk::FbxCluster*>& meshClusters)
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
                int bondeIndex = boneNameMap[tools::toLower(inMesh.vertexWeights[vertexWeightIndex].boneName)];

                meshClusters[bondeIndex]->AddControlPointIndex(vertexIndex, weight);
            }

            timedLoggerMsg.PrintDone();
        }

        static void InitClusters(std::vector<fbxsdk::FbxCluster*>& meshClusters, std::vector<BoneInfo>& bones, fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh, std::vector<fbxsdk::FbxNode*>& boneNodes)
        {
            meshClusters.resize(bones.size());
            for (size_t clusterIndex = 0; clusterIndex < bones.size(); clusterIndex++)
            {
                meshClusters[clusterIndex] = fbxsdk::FbxCluster::Create(poFbxScene, (inMesh.meshName + "__CLUSTER").c_str());
                meshClusters[clusterIndex]->SetLink(boneNodes[clusterIndex]);

                meshClusters[clusterIndex]->SetLinkMode(fbxsdk::FbxCluster::ELinkMode::eTotalOne);
            }
        }
        ;

        //private:
        //    static bool CheckParamsAndLog(
        //        fbxsdk::FbxScene* poFbxScene,
        //        fbxsdk::FbxNode* pMeshNode,
        //        const PackedMesh& inMesh,
        //        SceneContainer sceneContainer)
        //    {
        //        if (!poFbxScene)
        //        {
        //            LogActionError("poFbxScene == nullptr");
        //            return false;
        //        }

        //        if (!pMeshNode)
        //        {
        //            LogActionError("Node == nullptr");
        //            return false;
        //        }

        //        // get the mesh from the node
        //        auto pMesh = pMeshNode->GetMesh();

        //        if (!pMesh)
        //        {
        //            std::string nodeName = pMeshNode->GetName();
        //            LogActionError("Could node find mesh data for node: " + nodeName);
        //            return false;
        //        }

        //        if (bones.empty() || boneNodes.empty())
        //        {
        //            std::string nodeName = pMesh->GetName();
        //            LogActionError("bones or fbxnodes empty for node: " + nodeName);
        //            return false;
        //        }

        //        return true;
        //    }

    };

};