#pragma once

#include <vector>
#include <map>
#include <string.h> 
#include <string> 
#include <fbxsdk.h>

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\DataStructures\SceneContainer\SceneContainer.h"

namespace wrapdll
{
    class FBXSkeletonFactory
    {
    public:
        static std::vector<fbxsdk::FbxNode*> CreateSkeleton(fbxsdk::FbxScene* pScene, SceneContainer& sceneContainer)
        {
            // -- populate array with new skeleton bones

            auto& boneNodes = sceneContainer.GetFbxBoneNodes();
            auto& bones = sceneContainer.GetBones();

            boneNodes.clear();;
            boneNodes.resize(bones.size());

            for (size_t boneInfoIndex = 0; boneInfoIndex < bones.size(); boneInfoIndex++)
            {
                if (boneInfoIndex == 0)
                    boneNodes[boneInfoIndex] = CreateBone(pScene, bones[boneInfoIndex], fbxsdk::FbxSkeleton::eRoot);
                else
                    boneNodes[boneInfoIndex] = CreateBone(pScene, bones[boneInfoIndex]);

                if (bones[boneInfoIndex].parentId != -1)
                {
                    boneNodes[bones[boneInfoIndex].parentId]->AddChild(boneNodes[boneInfoIndex]);
                }                
            }                

            pScene->GetRootNode()->AddChild(boneNodes[0]); // add skeleton root to fbx scene graph                                  

            return boneNodes;
        }             

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


        static void CreateBindPose(FbxScene* pScene, std::vector<FbxNode*>& fbxBones)
        {
            FbxPose* lPose = FbxPose::Create(pScene, "BindPose");                 
        

            // Set the pose type as bind pose
            lPose->SetIsBindPose(true);

            for (uint32_t i = 0; i < fbxBones.size(); i++)
            {
                auto* lKFbxNode = fbxBones[i];
                FbxMatrix lBindMatrix = lKFbxNode->EvaluateGlobalTransform();

                // Add the node and its global transform matrix to the pose                
                lPose->Add(lKFbxNode, lBindMatrix);
            }
                       
            // Add the pose to the scene
            auto bAddBindPoseResult = pScene->AddPose(lPose);
        }

    private:
        static fbxsdk::FbxNode* CreateBone(fbxsdk::FbxScene* pScene, BoneInfo boneInfo, fbxsdk::FbxSkeleton::EType boneType = fbxsdk::FbxSkeleton::eLimb)
        {
            auto pNode = fbxsdk::FbxNode::Create(pScene, boneInfo.name);

            // create skeleton node attribute
            fbxsdk::FbxSkeleton* pSkeletonNode = fbxsdk::FbxSkeleton::Create(pScene, "");

            pSkeletonNode->SetSkeletonType(boneType);
            pSkeletonNode->Size.Set(20);
            pSkeletonNode->SetLimbNodeColor(FbxColor(0.0, 1.0, 1.0));

            pNode->SetTransformationInheritType(fbxsdk::FbxTransform::EInheritType::eInheritRrSs);

            pNode->QuaternionInterpolate.Set(fbxsdk::EFbxQuatInterpMode::eQuatInterpSlerp);
            pNode->SetPivotState(FbxNode::eSourcePivot, FbxNode::ePivotActive);
            pNode->SetRotationOrder(FbxNode::eSourcePivot, eSphericXYZ);
            pNode->SetUseRotationSpaceForLimitOnly(FbxNode::eSourcePivot, false);
            pNode->SetQuaternionInterpolation(FbxNode::eSourcePivot, eQuatInterpClassic);

            pNode->SetNodeAttribute(pSkeletonNode);

            auto scaleFactorTester = pScene->GetGlobalSettings().GetSystemUnit().GetConversionFactorFrom(fbxsdk::FbxSystemUnit::m);

            pNode->LclTranslation.Set(
                {
                   -boneInfo.localTranslation.x * scaleFactorTester,
                   boneInfo.localTranslation.y * scaleFactorTester,
                   boneInfo.localTranslation.z * scaleFactorTester,

                });

            FbxVector4 vEulerAngles;
            vEulerAngles.SetXYZ({ boneInfo.localRotation.x,-boneInfo.localRotation.y, -boneInfo.localRotation.z, boneInfo.localRotation.w });
            pNode->LclRotation.Set(vEulerAngles);

            return pNode;
        };       

    };
};