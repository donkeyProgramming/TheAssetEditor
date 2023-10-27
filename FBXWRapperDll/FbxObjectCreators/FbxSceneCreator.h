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
        fbxsdk::FbxScene* CreateFbxScene(fbxsdk::FbxManager* poSdkManager, SceneContainer& sceneContainer) override
        {
            auto timerLogger = TimeLogAction::PrintStart("Making Scene");

            m_poFbxScene = FBXHelperFileUtil::CreateFbxScene(poSdkManager);
            SetUnits(sceneContainer);

            if (sceneContainer.HasSkeleton()) {
                FBXSkeletonFactory::CreateSkeleton(m_poFbxScene, sceneContainer);
                AddSkeletonIdNode(sceneContainer);
            }

            AddMeshes(sceneContainer);
            FBXSkinHelperUtil::StoreBindPose_ChildrenOfRootNode(m_poFbxScene);

            timerLogger.PrintDone();

            return m_poFbxScene;
        }

    private:
        void SetUnits(SceneContainer& sceneContainer)
        {
            FBXUnitHelper::SetFbxSystmedUnit(m_poFbxScene, fbxsdk::FbxSystemUnit::cm);
            sceneContainer.SetDistanceScaleFactor(FBXUnitHelper::GetDistanceDataScaleFactor(m_poFbxScene));
        }

        void AddMeshes(SceneContainer& sceneContainer)
        {            
            FbxMaterialPhongCreator materialCreator;
            FbxMeshUnindexedCreator unindexedMeshCreator;

            for (auto& inPackedMesh : sceneContainer.GetMeshes())
            {
                // create unindexed mesh from the input meshes in the SceneContainer
                auto poFbxMesh = unindexedMeshCreator.Create(m_poFbxScene, inPackedMesh, sceneContainer);

                auto poFbxNode = fbxsdk::FbxNode::Create(m_poFbxScene, inPackedMesh.meshName.c_str());                
                poFbxNode->SetNodeAttribute(poFbxMesh);

                auto poFbxMaterial = materialCreator.CreateMaterial(m_poFbxScene, inPackedMesh.meshName);                                             
                poFbxNode->AddMaterial(poFbxMaterial);
                                
                if (sceneContainer.HasSkeleton())  {
                    FBXMeshSkinCreator::AddAddSkinningToFbxMesh(m_poFbxScene, poFbxNode, inPackedMesh, sceneContainer);
                }
                
                m_poFbxScene->GetRootNode()->AddChild(poFbxNode);
            }
        }

        void AddSkeletonIdNode(const SceneContainer& sceneContainer)
        {
            if (!sceneContainer.HasSkeleton()) {
                return;
            }

            const std::string nodePrefix = "skeleton//";
            auto poFbxNode = fbxsdk::FbxNode::Create(m_poFbxScene, (nodePrefix + sceneContainer.GetSkeletonName()).c_str());
            auto poFbxSkeleton = fbxsdk::FbxSkeleton::Create(m_poFbxScene, "");
            poFbxNode->SetNodeAttribute(poFbxSkeleton);

            m_poFbxScene->GetRootNode()->AddChild(poFbxNode);
        }

        // TODO: remove?
        //bool AddMesh(const PackedMesh& inputMesh, const std::string& nodeName);
        //bool AddNode(const std::string& nodeName, fbxsdk::FbxNodeAttribute::EType nodeType = fbxsdk::FbxNodeAttribute::EType::eMesh);
        //bool BuildSkeleton(std::vector<BoneInfo> bones);

    };

    // TODO: remove and/or move this code, if I decide to use a builder, it might be good to have saved the code

    //class FbxSceneBuilder
    //{
    //public:
    //    void Init(fbxsdk::FbxManager* poManager)
    //    {
    //        m_poFbxScene = fbxsdk::FbxScene::Create(poManager, "My Scene");
    //        if (!m_poFbxScene)
    //        {
    //            FBXSDK_printf("Error: Unable to create FBX scene!\n");
    //            exit(1);
    //        }
    //    }

    //    float SetUnits()
    //    {
    //        FBXUnitHelper::SetFbxSystmedUnit(m_poFbxScene, fbxsdk::FbxSystemUnit::Inch);
    //        return FBXUnitHelper::GetDistanceDataScaleFactor(m_poFbxScene);
    //    }

    //    void AddSkeleton(fbxsdk::FbxScene* poFbxScene, SceneContainer& sceneContainer, float scaleFactor)
    //    {
    //        FBXSkeletonFactory::CreateSkeleton(m_poFbxScene, sceneContainer);
    //    }

    //    void AddMeshes(const SceneContainer& sceneContainer, float scaleFactor)
    //    {
    //        for (auto& inMesh : sceneContainer.GetMeshes())
    //        {
    //            auto poFbxMesh = FbxMeshBuildingDirector::CreateFbxMesh(
    //                poFbxScene,
    //                inMesh,
    //                sceneContainer,
    //                scaleFactor);

    //            auto fbxNode = fbxsdk::FbxNode::Create(m_poFbxScene, inMesh.meshName.c_str());
    //            poFbxScene->GetRootNode()->AddChild(fbxNode);
    //        }
    //    }

    //    // TODO: finish
    //    void AddAnimation();

    //    fbxsdk::FbxScene* GetResult() { return m_poFbxScene; };

    //private:
    //    fbxsdk::FbxScene* m_poFbxScene = nullptr;
    //};

    //class FbxSceneBuilderDirector
    //{
    //    fbxsdk::FbxScene* CreateFbxScene(fbxsdk::FbxManager* poManager, SceneContainer& sceneContainer)
    //    {
    //        FbxSceneBuilder fbxSceneBuilder;

    //        fbxSceneBuilder.Init(poManager);
    //        auto scaleFactor = fbxSceneBuilder.SetUnits();
    //        // TODO: AddSkeleton
    //        // TODO: AddMeshes
    //        // TODO: AddAnimation

    //        return fbxSceneBuilder.GetResult();
    //    }
    //};


}

