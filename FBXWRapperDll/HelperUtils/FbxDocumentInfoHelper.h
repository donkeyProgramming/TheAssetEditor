#pragma once

#include <fbxsdk.h>
#include "..\..\Logging\Logging.h"

// TODO: finish
class FbxDocumentInfoHelper
{



    static bool SetFbxSceneDocumentInfo(FbxScene* pScene)
    {
        // create scene info
        fbxsdk::FbxDocumentInfo* sceneInfo = FbxDocumentInfo::Create(pScene, "SceneInfo");
        sceneInfo->mTitle = "Exported SCene";
        sceneInfo->mSubject = "Exported From Asset Editor";
        sceneInfo->mAuthor = "Phazer";
        sceneInfo->mRevision = "rev. 1.0";
        sceneInfo->mKeywords = "AssetEditor";
        sceneInfo->mComment = "Scene export from AssetEditor";

        // we need to add the sceneInfo before calling AddThumbNailToScene because
        // that function is asking the scene for the sceneInfo.
        pScene->SetSceneInfo(sceneInfo);

        //AddThumbnailToScene(pScene);

        //FbxNode* lPatch = CreatePatch(pScene, "Patch");
        //FbxNode* lSkeletonRoot = CreateSkeleton(pScene, "Skeleton");


        //// Build the node tree.
        //FbxNode* lRootNode = pScene->GetRootNode();
        //lRootNode->AddChild(lPatch);
        //lRootNode->AddChild(lSkeletonRoot);

        //// Store poses
        //LinkPatchToSkeleton(pScene, lPatch, lSkeletonRoot);
        //StoreBindPose(pScene, lPatch);
        //StoreRestPose(pScene, lSkeletonRoot);

        //// Animation
        //AnimateSkeleton(pScene, lSkeletonRoot);

        return true;
    }




};

