#include "..\..\DataStructures\SceneContainer\SceneContainer.h"
#include "..\..\HelperUtils\Tools.h"
#include "..\..\HelperUtils\Geometry\FBXSkinHelperUtil.h"
#include "..\..\ErrorCkecking\ErrorChecking.h"
#include "..\..\Logging\Logging.h"

#include <string> 
#include <map>
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
         bool CreateSkin(fbxsdk::FbxScene* poFbxScene, fbxsdk::FbxNode* pFbxNodeMesh, const PackedMesh& inMesh, SceneContainer& sceneContainer);

         void CheckSkinWeights(fbxsdk::FbxMesh* poMesh, fbxsdk::FbxSkin* pSkin);

    private:
        bool FillFbxSkin(fbxsdk::FbxSkin* pSkin, const PackedMesh& inMesh);
        void AssignGeomtry(fbxsdk::FbxNode* pMeshNode, std::vector<fbxsdk::FbxNode*>& boneNodes);
        void AddWeghts(const PackedMesh& inMesh, std::vector<BoneInfo>& bones);
        void InitClusters(fbxsdk::FbxScene* poFbxScene, PackedMesh inMesh, const std::vector<BoneInfo>& bones, const std::vector<fbxsdk::FbxNode*>& boneNodes);        

    private:
        std::vector<fbxsdk::FbxCluster*> m_meshFbxClusters;              
    };
};


