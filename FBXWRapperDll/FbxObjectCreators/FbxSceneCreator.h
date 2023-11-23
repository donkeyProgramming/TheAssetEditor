#include <fbxsdk.h>
#include "..\..\fbxsdk\common\Common.h"

#include "..\DataStructures\SceneContainer\SceneContainer.h"
#include "..\FbxObjectCreators\FbxMeshCreator.h"
#include "..\FbxObjectCreators\FBXSkeletonFactory.h"
#include "..\FbxObjectCreators\FBXMeshSkinCreator.h"
#include "..\FbxObjectCreators\FbxMaterialCreator.h"
#include "..\HelperUtils\FBXHelperFileUtil.h"

#pragma once
namespace wrapdll
{
    class IFbxSceneCreator
    {
    public:
        virtual fbxsdk::FbxScene* CreateFbxScene(fbxsdk::FbxManager* poSdkManager, SceneContainer& sceneContainer) = 0;
    
    protected:
        fbxsdk::FbxScene* m_poFbxScene = nullptr;
    };

    class FbxSceneCreator : public IFbxSceneCreator
    {
    public:
        fbxsdk::FbxScene* CreateFbxScene(fbxsdk::FbxManager* poSdkManager, SceneContainer& sceneContainer) override;

    private:
        void SetUnits(SceneContainer& sceneContainer);
        void AddMeshes(SceneContainer& sceneContainer);
        void AddSkeletonIdNode(const SceneContainer& sceneContainer);        
    };
}

