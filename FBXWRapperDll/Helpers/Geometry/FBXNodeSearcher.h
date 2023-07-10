#pragma once

#include "../../Helpers/Tools.h"
#include "../../Helpers/Geometry/FBXMeshGeometryHelper.h"
 

namespace wrapdll
{
	class FBXNodeSearcher
	{
	public:
		static std::string FetchSkeletonNameFromScene(fbxsdk::FbxScene* pScene)

		{
			std::string tempSkeletonString = "";
			auto parent = pScene->GetRootNode();

			SearchNodesForSkeletonTagRecursive(parent, tempSkeletonString);

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

	private:	
		static void SearchNodesForSkeletonTagRecursive(fbxsdk::FbxNode* parent, std::string& skeletonString)
		{
			if (!skeletonString.empty()) // to make sure the recursive stops when string is set
				return;

			const std::string nodeTag = "skeleton//"; // a node int scenegraph starts witth these char, if skelton info is set by the export

			for (int nodeIndex = 0; nodeIndex < parent->GetChildCount(); nodeIndex++)
			{
				auto currentChildNode = parent->GetChild(nodeIndex);

				std::string nodeName = currentChildNode->GetName();

				if (std::tolower(nodeName).find(nodeTag) == 0)
				{
					skeletonString = nodeName.erase(0, nodeTag.length());

					return;
				}

				SearchNodesForSkeletonTagRecursive(currentChildNode, skeletonString);
			}
		}

		static void FindFbxMeshesRecursive(fbxsdk::FbxNode* poParent, std::vector<fbxsdk::FbxMesh*>& fbxMeshes)
		{
			for (int childBoneIndex = 0; childBoneIndex < poParent->GetChildCount(); ++childBoneIndex)
			{
				fbxsdk::FbxNode* poChildItem = poParent->GetChild(childBoneIndex);

				if (poChildItem)
				{
					auto poFbxNodeAtrribute = poChildItem->GetNodeAttribute();
					if (poFbxNodeAtrribute) // valid node attribute?
					{
						if (poFbxNodeAtrribute->GetAttributeType() == fbxsdk::FbxNodeAttribute::EType::eMesh) // node has "eMesh" attribute, so should contain mesh object
						{
							fbxsdk::FbxMesh* poMeshNode = (fbxsdk::FbxMesh*)poChildItem->GetNodeAttribute(); // get mesh objec ptr

							if (poMeshNode)
							{
								fbxMeshes.push_back(poMeshNode);
							}
						}
					}
				}

				// recurse
				FindFbxMeshesRecursive(poChildItem, fbxMeshes);
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