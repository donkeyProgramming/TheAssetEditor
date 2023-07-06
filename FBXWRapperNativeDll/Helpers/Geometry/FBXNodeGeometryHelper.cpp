#include "FBXNodeGeometryHelper.h"
#include <vector>
#include <map>

using namespace wrapdll;

FbxAMatrix FBXNodeGeometryHelper::GetNodeGeometryTransform(FbxNode* pNode)
{
	FbxAMatrix matrixGeo;
	matrixGeo.SetIdentity();

	if (pNode->GetNodeAttribute())
	{
		const FbxVector4 lT = pNode->GetGeometricTranslation(FbxNode::eSourcePivot);
		const FbxVector4 lR = pNode->GetGeometricRotation(FbxNode::eSourcePivot);
		const FbxVector4 lS = pNode->GetGeometricScaling(FbxNode::eSourcePivot);

		matrixGeo.SetT(lT);
		matrixGeo.SetR(lR);
		matrixGeo.SetS(lS);
	}

	return matrixGeo;
}

FbxAMatrix FBXNodeGeometryHelper::GetNodeWorldTransform(FbxNode* pNode)
{
	FbxAMatrix matrixL2W;
	matrixL2W.SetIdentity();

	if (NULL == pNode)
	{
		return matrixL2W;
	}

	matrixL2W = pNode->EvaluateGlobalTransform();

	FbxAMatrix matrixGeo = GetNodeGeometryTransform(pNode);

	matrixL2W *= matrixGeo;

	// todo remove debugging code
	//matrixL2W.SetIdentity();

	return matrixL2W;
}

FbxAMatrix FBXNodeGeometryHelper::GetNodeWorldTransform_Normals(FbxNode* pNode)
{
	FbxAMatrix matrixL2W;
	matrixL2W.SetIdentity();

	if (NULL == pNode)
	{
		return matrixL2W;
	}
	matrixL2W.SetQOnly(GetNodeWorldTransform(pNode).GetQ());

	return matrixL2W;
}