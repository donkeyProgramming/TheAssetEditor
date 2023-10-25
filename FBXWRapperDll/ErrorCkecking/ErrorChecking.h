#pragma once

#include <fbxsdk.h>
#include "..\DataStructures\PackedMeshStructs.h"


class ErrorChecking
{
public:
    static bool CheckParamsAndLog(fbxsdk::FbxScene* poFbxScene, fbxsdk::FbxNode* pMeshNode)
    {
        if (poFbxScene == nullptr)  return LogActionError("(poFbxScene == nullptr)");
        if (pMeshNode == nullptr)  return LogActionError("poFbxMesh == nullptr");
        if (!pMeshNode->GetMesh()) return LogActionError("pMesh->GetNode() == nulptr");

        return true;
    };

    static bool CheckParamsAndLog(fbxsdk::FbxScene* poFbxScene, fbxsdk::FbxMesh* pMesh)
    {
        if (poFbxScene == nullptr)  return LogActionError("(poFbxScene == nullptr)");
        if (pMesh == nullptr)  return LogActionError("poFbxMesh == nullptr)");
        if (!pMesh->GetNode()) return LogActionError("pMesh->GetNode() == nulptr");

        return true;
    };

};
       