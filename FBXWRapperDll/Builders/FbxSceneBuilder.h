#include <fbxsdk.h>

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\DataStructures\SceneContainer\SceneContainer.h"

#pragma once
namespace wrapdll
{
class FbxSceneBuilder
{
public:
    static fbxsdk::FbxScene* MakeFbxSceneFromContainer(FbxManager* m_poSDKManager, SceneContainer* pSceneContainer)
    {
        
    }

    


    bool AddMesh(const PackedMesh& inputMesh, const std::string& nodeName);
    bool AddNode(const std::string& nodeName, fbxsdk::FbxNodeAttribute::EType nodeType = fbxsdk::FbxNodeAttribute::EType::eMesh);
    bool BuildSkeleton(std::vector<BoneInfo> bones);

private:
    fbxsdk::FbxScene* m_poFbxScene;
};
}

