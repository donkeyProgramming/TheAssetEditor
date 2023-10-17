#include "..\Processing\MeshProcessor.h"
#include "..\Helpers\Tools.h"
#include <string> 

#pragma once
class FBXMeshBuilder
{
public:



    void AddControlPointsUnindex(const PackedMesh& inMesh)
    {
        // TODO: look in FBX SDK example, to see how they suggest building meshes
       
        // convert mesh data to unindexed, so triangle corner 0,1,2  uses vertex 0,1,2, and so on

        // add control points to mesh
        // add normals to mesh
        // add Uvs to mesh

        // add triangles to mesh
    }
private:

    // TODO: remove?
  /*  std::vector<PackedCommonVertex> GetTriangle(const PackedMesh& inMesh, size_t triangleIndex)
    {
        std::vector<PackedCommonVertex> outTriangle(3);

        for (size_t corner = 0; corner < 3; corner++)
        {
            uint16_t cornerIndex = inMesh.indices[3 * triangleIndex + corner];
            const PackedCommonVertex& cornerVertex = inMesh.vertices[cornerIndex];

            outTriangle.push_back(cornerVertex);
        }
    }*/
    
    PackedMesh MakeUnindexedPackedMesh(const PackedMesh& inMesh)
    {
        auto i = 0;       

        PackedMesh outMesh;


        outMesh.meshName = inMesh.meshName;
        
        for (size_t triangleIndex = 0; triangleIndex < inMesh.indices.size() / 3; triangleIndex++)
        {
            for (size_t corner = 0; corner < 3; corner++)
            {
                auto cornerIndex = inMesh.indices[3 * triangleIndex + corner];
                auto& cornerVertex = inMesh.vertices[cornerIndex];

                outMesh.vertices.push_back(cornerVertex);
            }
        }
        
        return outMesh;
    }

    static int IndexOfBoneName(const std::vector<BoneInfo>& bones, std::string name)
    {
        for (size_t boneIndex = 0; boneIndex < bones.size(); boneIndex++)
        {
            if (tools::toLower(bones[boneIndex].name) == tools::toLower(name))
            {
                return boneIndex;
            }
        }
        return -1;
    }

    //static bool AddRigging(
    //    fbxsdk::FbxScene* poFbxScene, 
    //    fbxsdk::FbxNode* pMeshNode, 
    //    const PackedMesh& inMesh, 
    //    const std::vector<VertexWeight>& vertexWeights,
    //    const std::vector<BoneInfo>& bones,
    //    const std::vector<fbxsdk::FbxNode*> boneNodes)
    //{
    //    
    //    //auto& _vecVertices = inMesh.vertices;

    //    if (!pMeshNode)
    //    {
    //        LogActionError("Node == nullptr");
    //        return false;
    //    }

    //    // get the mesh from the node
    //    auto pMesh = pMeshNode->GetMesh();

    //    if (!pMesh)
    //    {
    //        std::string nodeName = pMesh->GetName();
    //        LogActionError("Could node find mesh data for node: " + nodeName);
    //        return false;
    //    }

    //    std::vector<FbxCluster*> vecClusters;
    //    vecClusters.resize(bones.size());

    //    //map<int, int> mapJointWeightCount;
    //    for (size_t i = 0; i < bones.size(); i++)
    //    {
    //        vecClusters[i] = FbxCluster::Create(poFbxScene, (inMesh.meshName + "__CLUSTER").c_str());
    //        vecClusters[i]->SetLink(boneNodes[i]);

    //        vecClusters[i]->SetLinkMode(FbxCluster::ELinkMode::eTotalOne);
    //        //	mapJointWeightCount[i] = 0;
    //    }

    //    // run trough all vertices
    //    for (size_t i = 0; i < vertexWeights.size(); i++)
    //    {
    //       float weight = vertexWeights[i].weight;
    //       int vertexIndex = vertexWeights[i].vertexIndex;
    //       int bondeIndex = IndexOfBoneName(bones, vertexWeights[i].boneName);           

    //       vecClusters[bondeIndex]->AddControlPointIndex(vertexIndex, weight);
    //    }

    //    // asign global matrices to clusters (cluster = bone)
    //    FbxAMatrix lXMatrixStatic = pMeshNode->EvaluateGlobalTransform();
    //    int siz = vecClusters.size();
    //    for (size_t i = 0; i < vecClusters.size(); i++)
    //    {
    //        vecClusters[i]->SetTransformMatrix(lXMatrixStatic);

    //        FbxAMatrix lXMatrix = boneNodes[i]->EvaluateGlobalTransform();
    //        vecClusters[i]->SetTransformLinkMatrix(lXMatrix);
    //    }

    //    FbxSkin* pSkin = FbxSkin::Create(poFbxScene, (inMesh.meshName + + "_skin  ").c_str());
    //    for (size_t i = 0; i < vecClusters.size(); i++)
    //    {
    //        pSkin->AddCluster(vecClusters[i]);
    //    }

    //    // add this set of influences to that mesh
    //    int index1 = pMesh->AddDeformer(pSkin);

    //    int count2 = pMesh->GetDeformerCount();
    //    int lSkinCount = pMesh->GetDeformerCount(FbxDeformer::eSkin);
    //    int count = pSkin->GetClusterCount();

    //    bool bPoseResult = StoreBindPose(m_pScene, pMeshNode);
    //}

private:
    fbxsdk::FbxMesh* m_poFbxMesh = nullptr;
};

