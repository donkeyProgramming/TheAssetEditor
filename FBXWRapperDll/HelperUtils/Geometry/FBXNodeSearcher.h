#pragma once

#include "../..\HelperUtils\Tools.h"
#include "../Geometry/FBXMeshGeometryHelper.h"

namespace wrapdll
{
    class FBXNodeSearcher
    {
    public:
        static std::string FetchSkeletonNameFromScene(fbxsdk::FbxScene* pScene, bool* pIsAttributeSkeleton = nullptr)
        {
            std::string tempSkeletonString = "";
            auto parent = pScene->GetRootNode();

            SearchNodesForSkeletonTagRecursive(parent, tempSkeletonString, pIsAttributeSkeleton);

            return tempSkeletonString;
        }

        static bool FindMeshesInScene(fbxsdk::FbxScene* poScene, std::vector<fbxsdk::FbxMesh*>& fbxMeshes)
        {
            if (!poScene)
                return false;

            auto poRootNode = poScene->GetRootNode();

            if (!poRootNode)
                return false;

            FindFbxMeshesRecursive(poRootNode, fbxMeshes);

            return true;
        }

        static bool FindFbxNodesByType(fbxsdk::FbxNodeAttribute::EType nodeType, fbxsdk::FbxScene* poScene, std::vector<fbxsdk::FbxNode*>& fbxMeshes)
        {
            if (!poScene)
                return false;

            auto poRootNode = poScene->GetRootNode();

            if (!poRootNode)
                return false;

            FindFbxNodeByTypeRecursive(nodeType, poRootNode, fbxMeshes);

            return true;
        }

        static void FindAllNodes(fbxsdk::FbxNode* poParent, std::vector<fbxsdk::FbxNode*>& fbxMeshes)
        {
            for (int childBoneIndex = 0; childBoneIndex < poParent->GetChildCount(); ++childBoneIndex)
            {
                fbxsdk::FbxNode* poChildItem = poParent->GetChild(childBoneIndex);

                if (poChildItem)
                {
                    fbxMeshes.push_back(poChildItem);
                    // FidnAllNodes
                    FindAllNodes(poChildItem, fbxMeshes);
                }
            }
        }

    private:
        static void SearchNodesForSkeletonTagRecursive(fbxsdk::FbxNode* parent, std::string& skeletonString, bool* pIsBoneAtrribute)
        {
            if (pIsBoneAtrribute)
            {
                *pIsBoneAtrribute = false;
            }

            if (!skeletonString.empty()) // to make sure the recursive stops when string is set
                return;

            const std::string nodeTag = "skeleton//"; // a node int scenegraph starts witth these char, if skelton info is set by the export

            for (int nodeIndex = 0; nodeIndex < parent->GetChildCount(); nodeIndex++)
            {
                auto currentChildNode = parent->GetChild(nodeIndex);

                std::string nodeName = currentChildNode->GetName();

                if (tools::toLower(nodeName).find(tools::toLower(nodeTag)) == 0)
                {
                    skeletonString = nodeName.erase(0, nodeTag.length());                    

                    if (pIsBoneAtrribute)
                    {
                        auto attributeType = currentChildNode->GetNodeAttribute()->GetAttributeType();
                        if (attributeType == fbxsdk::FbxNodeAttribute::eSkeleton)
                        {
                            *pIsBoneAtrribute = true;
                        }
                    }
                    return;
                }

                SearchNodesForSkeletonTagRecursive(currentChildNode, skeletonString, pIsBoneAtrribute);
            }
        }

        static void FindFbxMeshesRecursive(fbxsdk::FbxNode* poParent, std::vector<fbxsdk::FbxMesh*>& fbxMeshes)
        {
            for (int childBoneIndex = 0; childBoneIndex < poParent->GetChildCount(); ++childBoneIndex)
            {
                fbxsdk::FbxNode* poChildNode= poParent->GetChild(childBoneIndex);

                if (poChildNode)
                {
                    auto poFbxNodeAtrribute = poChildNode->GetNodeAttribute();
                    if (poFbxNodeAtrribute) // valid node attribute?
                    {
                        if (poFbxNodeAtrribute->GetAttributeType() == fbxsdk::FbxNodeAttribute::EType::eMesh) // node has "eMesh" attribute, so should contain mesh object
                        {
                            fbxsdk::FbxMesh* poMeshNode = (fbxsdk::FbxMesh*)poChildNode->GetNodeAttribute(); // get mesh objec ptr

                            if (poMeshNode)
                            {
                                fbxMeshes.push_back(poMeshNode);
                            }
                        }
                    }
                }

                // recurse
                FindFbxMeshesRecursive(poChildNode, fbxMeshes);
            }
        }

        static void FindFbxNodeByTypeRecursive(fbxsdk::FbxNodeAttribute::EType nodeType, fbxsdk::FbxNode* poParent, std::vector<fbxsdk::FbxNode*>& fbxMeshes)
        {
            for (int childBoneIndex = 0; childBoneIndex < poParent->GetChildCount(); ++childBoneIndex)
            {
                fbxsdk::FbxNode* poCurrentNode = poParent->GetChild(childBoneIndex);

                if (poCurrentNode)
                {
                    auto poFbxNodeAtrribute = poCurrentNode->GetNodeAttribute();
                    if (poFbxNodeAtrribute) // valid node attribute?
                    {
                        if (poFbxNodeAtrribute->GetAttributeType() == nodeType) // node has "eMesh" attribute, so should contain mesh object						{													{				
                        {
                            fbxMeshes.push_back(poCurrentNode);
                        }
                    }
                }

                // recurse
                FindFbxNodeByTypeRecursive(nodeType, poCurrentNode, fbxMeshes);
            }
        }
    };
}