#include "FbxSceneCreator.h"

fbxsdk::FbxScene* wrapdll::FbxSceneCreator::CreateFbxScene(fbxsdk::FbxManager* poSdkManager, SceneContainer& sceneContainer)
{
    auto timedLogger = TimeLogAction::PrintStart("Making FBX Scene");

    m_poFbxScene = FBXHelperFileUtil::CreateFbxScene(poSdkManager);
    SetUnits(sceneContainer);

    if (sceneContainer.HasSkeleton()) {
        FBXSkeletonFactory::CreateSkeleton(m_poFbxScene, sceneContainer);
        AddSkeletonIdNode(sceneContainer);
    }

    AddMeshes(sceneContainer);
    FBXSkinHelperUtil::StoreBindPose_ChildrenOfRootNode(m_poFbxScene);

    timedLogger.PrintDoneSuccess();

    return m_poFbxScene;
}

void wrapdll::FbxSceneCreator::SetUnits(SceneContainer& sceneContainer)
{
    FBXUnitHelper::SetFbxSystmedUnit(m_poFbxScene, fbxsdk::FbxSystemUnit::cm);
    sceneContainer.SetDistanceScaleFactor(FBXUnitHelper::GetDistanceDataScaleFactor(m_poFbxScene));
}

void wrapdll::FbxSceneCreator::AddMeshes(SceneContainer& sceneContainer)
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

        if (sceneContainer.HasSkeleton()) {
            FBXMeshSkinCreator().CreateSkin(m_poFbxScene, poFbxNode, inPackedMesh, sceneContainer);
        }

        m_poFbxScene->GetRootNode()->AddChild(poFbxNode);
    }
}

void wrapdll::FbxSceneCreator::AddSkeletonIdNode(const SceneContainer& sceneContainer)
{
    if (!sceneContainer.HasSkeleton()) {
        return;
    }

    const std::string nodePrefix = "skeleton//";
    auto poFbxNode = fbxsdk::FbxNode::Create(m_poFbxScene, (nodePrefix + sceneContainer.GetSkeletonName()).c_str());
    /*auto poFbxSkeleton = fbxsdk::FbxSkeleton::Create(m_poFbxScene, "");
    poFbxNode->SetNodeAttribute(poFbxSkeleton);*/

    m_poFbxScene->GetRootNode()->AddChild(poFbxNode);
}