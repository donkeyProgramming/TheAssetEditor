#pragma once
#include <fbxsdk.h>
#include <vector>
#include <map>

namespace wrapdll
{
	class FBXNodeGeometryHelper
	{
	public:
		static fbxsdk::FbxAMatrix GetNodeGeometryTransform(fbxsdk::FbxNode* pNode);
		static fbxsdk::FbxAMatrix GetNodeWorldTransform(fbxsdk::FbxNode* pNode);
		static fbxsdk::FbxAMatrix GetNodeWorldTransformNormals(fbxsdk::FbxNode* pNode);
	};
}
