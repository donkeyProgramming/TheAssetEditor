#pragma once
#include "..\..\.\HelperUtils\Tools.h"
#include "..\..\DataStructures/PackedMeshStructs.h"
#include <functional>
#include <fbxsdk.h>

// Store the Bind Pose
namespace wrapdll
{
    class FBXSkinHelperUtil
    {
    public:
        static int IndexOfBoneName(const std::vector<BoneInfo>& bones, std::string name)
        {
            for (int boneIndex = 0; boneIndex < bones.size(); boneIndex++)
            {
                if (tools::toLower(bones[boneIndex].name) == tools::toLower(name))
                {
                    return boneIndex;
                }
            }
            return -1;
        }

    public:
        // Store the Bind Pose
        static void StoreBindPose_ChildrenOfRootNode(fbxsdk::FbxScene* pScene)
        {
            using namespace fbxsdk;
            // In the bind pose, we must store all the link's global matrix at the time of the bind.
            // Plus, we must store all the parent(s) global matrix of a link, even if they are not
            // themselves deforming any model.
            // In this example, since there is only one model deformed, we don't need walk through 
            // the scene
            //
            // Now list the all the link involve in the patch deformation


            FbxArray<FbxNode*> lClusteredFbxNodes;
            int                       i, j;

            auto childCount = pScene->GetRootNode()->GetChildCount();
            for (int nodeIndex = 0; nodeIndex  < childCount; nodeIndex++)
            {
                auto pPatch = pScene->GetRootNode()->GetChild(nodeIndex);



                //std::function<void(FbxArray<FbxNode*>&, FbxNode*)> AddNodeRecursivelyLambda;

                //AddNodeRecursivelyLambda = [=](FbxArray<FbxNode*>& pNodeArray, FbxNode* pNode)
                //    {
                //        if (pNode)
                //        {
                //            AddNodeRecursivelyLambda(pNodeArray, pNode->GetParent());
                //            if (pNodeArray.Find(pNode) == -1)
                //            {
                //                // Node not in the list, add it
                //                pNodeArray.Add(pNode);
                //            }
                //        }
                //    };


                
                if (pPatch && pPatch->GetNodeAttribute())
                {
                    int lSkinCount = 0;
                    int lClusterCount = 0;
                    switch (pPatch->GetNodeAttribute()->GetAttributeType())
                    {
                    default:
                        break;
                    case FbxNodeAttribute::eMesh:
                    case FbxNodeAttribute::eNurbs:
                    case FbxNodeAttribute::ePatch:
                        lSkinCount = ((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformerCount(FbxDeformer::eSkin);
                        //Go through all the skins and count them
                        //then go through each skin and get their cluster count
                        for (i = 0; i < lSkinCount; ++i)
                        {
                            FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
                            lClusterCount += lSkin->GetClusterCount();
                        }
                        break;
                    }
                    //if we found some clusters we must add the node
                    if (lClusterCount)
                    {
                        //Again, go through all the skins get each cluster link and add them
                        for (i = 0; i < lSkinCount; ++i)
                        {
                            FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
                            lClusterCount = lSkin->GetClusterCount();
                            for (j = 0; j < lClusterCount; ++j)
                            {
                                FbxNode* lClusterNode = lSkin->GetCluster(j)->GetLink();
                                AddNodeRecursively(lClusteredFbxNodes, lClusterNode);
                            }
                        }
                        // Add the patch to the pose
                        lClusteredFbxNodes.Add(pPatch);
                    }
                }
            }

            // Now create a bind pose with the link list
            if (lClusteredFbxNodes.GetCount())
            {
                // A pose must be named. Arbitrarily use the name of the patch node.
                FbxPose* lPose = FbxPose::Create(pScene, "Bind Pose");
                // default pose type is rest pose, so we need to set the type as bind pose
                lPose->SetIsBindPose(true);
                for (i = 0; i < lClusteredFbxNodes.GetCount(); i++)
                {
                    FbxNode* lKFbxNode = lClusteredFbxNodes.GetAt(i);
                    FbxMatrix lBindMatrix = lKFbxNode->EvaluateGlobalTransform();
                    lPose->Add(lKFbxNode, lBindMatrix);
                }

                // Add the pose to the scene
                auto result = pScene->AddPose(lPose);
                //TODO: REMOVE
                auto debug_break = 1;
            }
        }


        static bool StoreBindPose_INACTIVE(fbxsdk::FbxScene* pScene, fbxsdk::FbxNode* pPatch)
        {
            using namespace fbxsdk;

            if (pPatch == nullptr || pScene == nullptr)
            {
                return false;
            }

            // In the bind pose, we must store all the link's global matrix at the time of the bind.
            // Plus, we must store all the parent(s) global matrix of a link, even if they are not
            // themselves deforming any model.

            // In this example, since there is only one model deformed, we don't need walk through 
            // the scene
            //

            // Now list the all the link involve in the patch deformation
            FbxArray<FbxNode*> lClusteredFbxNodes;
            int i, j;

            if (pPatch && pPatch->GetNodeAttribute())
            {
                int lSkinCount = 0;
                int lClusterCount = 0;
                switch (pPatch->GetNodeAttribute()->GetAttributeType())
                {
                default:
                    break;
                case FbxNodeAttribute::eMesh:
                case FbxNodeAttribute::eNurbs:
                case FbxNodeAttribute::ePatch:

                    lSkinCount = ((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformerCount(FbxDeformer::eSkin);
                    //Go through all the skins and count them
                    //then go through each skin and get their cluster count
                    for (i = 0; i < lSkinCount; ++i)
                    {
                        FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
                        lClusterCount += lSkin->GetClusterCount();
                    }
                    break;
                }
                //if we found some clusters we must add the node
                if (lClusterCount)
                {
                    //Again, go through all the skins get each cluster link and add them
                    for (i = 0; i < lSkinCount; ++i)
                    {
                        FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
                        lClusterCount = lSkin->GetClusterCount();
                        for (j = 0; j < lClusterCount; ++j)
                        {
                            FbxNode* lClusterNode = lSkin->GetCluster(j)->GetLink();
                            AddNodeRecursively(lClusteredFbxNodes, lClusterNode);
                        }
                    }

                    // Add the patch to the pose
                    auto addresult = lClusteredFbxNodes.Add(pPatch);

                    if (addresult == -1)
                        return false;
                }
            }

            // Now create a bind pose with the link list
            if (lClusteredFbxNodes.GetCount())
            {
                // A pose must be named. Arbitrarily use the name of the patch node.
                auto lPose = fbxsdk::FbxPose::Create(pScene, (pPatch->GetName() + std::string("__pose")).c_str());

                // default pose type is rest pose, so we need to set the type as bind pose
                lPose->SetIsBindPose(true);

                for (i = 0; i < lClusteredFbxNodes.GetCount(); i++)
                {
                    FbxNode* lKFbxNode = lClusteredFbxNodes.GetAt(i);
                    FbxMatrix lBindMatrix = lKFbxNode->EvaluateGlobalTransform();

                    lPose->Add(lKFbxNode, lBindMatrix);
                }

                // Add the pose to the scene
                auto addPoseResult = pScene->AddPose(lPose);

                if (addPoseResult)
                    return true;
            }

            return false;
        }

    private:
        static void AddNodeRecursively(FbxArray<FbxNode*>& pNodeArray, FbxNode* pNode)
        {
            if (pNode)
            {
                AddNodeRecursively(pNodeArray, pNode->GetParent());

                if (pNodeArray.Find(pNode) == -1)
                {
                    // Node not in the list, add it
                    pNodeArray.Add(pNode);
                }
            }
        }
    public:
        static bool  StoreBindPose(fbxsdk::FbxScene* pScene, fbxsdk::FbxNode* pPatch)
        {
            using namespace fbxsdk;

            // In the bind pose, we must store all the link's global matrix at the time of the bind.
            // Plus, we must store all the parent(s) global matrix of a link, even if they are not
            // themselves deforming any model.

            // In this example, since there is only one model deformed, we don't need walk through
            // the scene
            //

            // Now list the all the link involve in the patch deformation
            FbxArray<FbxNode*> lClusteredFbxNodes;
            int                       i, j;

            if (!pPatch || !pPatch->GetNodeAttribute())
            {
                return false;
            }

            int lSkinCount = 0;
            int lClusterCount = 0;
            switch (pPatch->GetNodeAttribute()->GetAttributeType())
            {
            default:
                break;
            case FbxNodeAttribute::eMesh:
            case FbxNodeAttribute::eNurbs:
            case FbxNodeAttribute::ePatch:

                lSkinCount = ((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformerCount(FbxDeformer::eSkin);
                //Go through all the skins and count them
                //then go through each skin and get their cluster count
                for (i = 0; i < lSkinCount; ++i)
                {
                    FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
                    lClusterCount += lSkin->GetClusterCount();
                }
                break;
            }
            //if we found some clusters we must add the node
            if (!lClusterCount)
            {
                return false;
            }

            //Again, go through all the skins get each cluster link and add them
            for (i = 0; i < lSkinCount; ++i)
            {
                FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
                lClusterCount = lSkin->GetClusterCount();
                for (j = 0; j < lClusterCount; ++j)
                {
                    FbxNode* lClusterNode = lSkin->GetCluster(j)->GetLink();
                    AddNodeRecursively(lClusteredFbxNodes, lClusterNode);
                }
            }

            // Add the patch to the pose
            lClusteredFbxNodes.Add(pPatch);

            // Now create a bind pose with the link list
            auto lClusteredFbxNodesCount = lClusteredFbxNodes.GetCount();
            if (!lClusteredFbxNodesCount)
            {
                return false;
            }

            // A pose must be named. Arbitrarily use the name of the patch node.
            FbxPose* lPose = FbxPose::Create(pScene, pPatch->GetName());

            // default pose type is rest pose, so we need to set the type as bind pose
            lPose->SetIsBindPose(true);

            for (i = 0; i < lClusteredFbxNodes.GetCount(); i++)
            {
                FbxNode* lKFbxNode = lClusteredFbxNodes.GetAt(i);
                FbxMatrix lBindMatrix = lKFbxNode->EvaluateGlobalTransform();

                lPose->Add(lKFbxNode, lBindMatrix);
            }

            bool isValid = lPose->IsValidBindPose(pPatch);


            auto stored_count = lPose->GetCount();
            // Add the pose to the scene
            auto DEBUG_scenePoseCount = pScene->GetPoseCount();

            auto removePoseResult = pScene->RemovePose(0);

            auto addPoseResult = pScene->AddPose(lPose);

            return addPoseResult;


            return false;
        }


        // Store a Rest Pose
        static bool StoreRestPose(fbxsdk::FbxScene* pScene, std::vector<FbxNode*>& pSkeletonNodes)
        {
            // This example show an arbitrary rest pose assignment.
            // This rest pose will set the bone rotation to the same value 
            // as time 1 second in the first stack of animation, but the 
            // position of the bone will be set elsewhere in the scene.
            /*FbxString     lNodeName;
            FbxNode* lKFbxNode;
            FbxMatrix  lTransformMatrix;
            FbxVector4 lT, lR, lS(1.0, 1.0, 1.0);*/

            // Create the rest pose
            auto lPose = fbxsdk::FbxPose::Create(pScene, "RestPose Pose");

            // Add the skeleton root node to the pose
            auto fbxNodeBoneRoot = pSkeletonNodes[0];
            lPose->Add(fbxNodeBoneRoot, fbxNodeBoneRoot->EvaluateGlobalTransform(), false /*it's a global matrix*/);





            for (int boneNodeIndex = 1; boneNodeIndex < pSkeletonNodes.size(); boneNodeIndex++)
            {
                // Add the skeleton second node to the pose
                auto result = lPose->Add(
                    pSkeletonNodes[boneNodeIndex],
                    pSkeletonNodes[boneNodeIndex]->EvaluateLocalTransform(), true /*it's a local matrix*/);

            }
            
            // Now add the pose to the scene
            auto addResult = pScene->AddPose(lPose);

            return addResult;
        }

    };
}

//static bool  StoreBindPoseUsingSkeletonNodes(fbxsdk::FbxScene* pScene, std::vector<FbxNode*>& boneNodes)
        //{
        //    using namespace fbxsdk;

        //    // In the bind pose, we must store all the link's global matrix at the time of the bind.
        //    // Plus, we must store all the parent(s) global matrix of a link, even if they are not
        //    // themselves deforming any model.

        //    // In this example, since there is only one model deformed, we don't need walk through
        //    // the scene
        //    //

        //    // Now list the all the link involve in the patch deformation
        //    FbxArray<FbxNode*> lClusteredFbxNodes;
        //    int                       i, j;

        //    if (boneNodes.empty() || !pPatch->GetNodeAttribute())
        //    {
        //        return false;
        //    }

        //    int lSkinCount = 0;
        //    int lClusterCount = 0;
        //    switch (pPatch->GetNodeAttribute()->GetAttributeType())
        //    {
        //    default:
        //        break;
        //    case FbxNodeAttribute::eMesh:
        //    case FbxNodeAttribute::eNurbs:
        //    case FbxNodeAttribute::ePatch:

        //        lSkinCount = ((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformerCount(FbxDeformer::eSkin);
        //        //Go through all the skins and count them
        //        //then go through each skin and get their cluster count
        //        for (i = 0; i < lSkinCount; ++i)
        //        {
        //            FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
        //            lClusterCount += lSkin->GetClusterCount();
        //        }
        //        break;
        //    }
        //    //if we found some clusters we must add the node
        //    if (!lClusterCount)
        //    {
        //        return false;
        //    }

        //    //Again, go through all the skins get each cluster link and add them
        //    for (i = 0; i < lSkinCount; ++i)
        //    {
        //        FbxSkin* lSkin = (FbxSkin*)((FbxGeometry*)pPatch->GetNodeAttribute())->GetDeformer(i, FbxDeformer::eSkin);
        //        lClusterCount = lSkin->GetClusterCount();
        //        for (j = 0; j < lClusterCount; ++j)
        //        {
        //            FbxNode* lClusterNode = lSkin->GetCluster(j)->GetLink();
        //            AddNodeRecursively(lClusteredFbxNodes, lClusterNode);
        //        }
        //    }

        //    // Add the patch to the pose
        //    lClusteredFbxNodes.Add(pPatch);

        //    // Now create a bind pose with the link list
        //    if (!lClusteredFbxNodes.GetCount())
        //    {
        //        return false;
        //    }

        //    // A pose must be named. Arbitrarily use the name of the patch node.
        //    FbxPose* lPose = FbxPose::Create(pScene, pPatch->GetName());

        //    // default pose type is rest pose, so we need to set the type as bind pose
        //    lPose->SetIsBindPose(true);

        //    for (i = 0; i < lClusteredFbxNodes.GetCount(); i++)
        //    {
        //        FbxNode* lKFbxNode = lClusteredFbxNodes.GetAt(i);
        //        FbxMatrix lBindMatrix = lKFbxNode->EvaluateGlobalTransform();

        //        lPose->Add(lKFbxNode, lBindMatrix);
        //    }

        //    bool isValid = lPose->IsValidBindPose(pPatch);


        //    auto stored_count = lPose->GetCount();
        //    // Add the pose to the scene
        //    auto DEBUG_scenePoseCount = pScene->GetPoseCount();

        //    auto removePoseResult = pScene->RemovePose(0);

        //    auto addPoseResult = pScene->AddPose(lPose);

        //    return addPoseResult;


        //    return false;
        //}