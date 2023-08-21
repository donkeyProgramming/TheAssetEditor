#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"
#include "..\Logging\Logging.h"
#include "..\Helpers\Geometry\FBXNodeGeometryHelper.h"
#include "..\Helpers\Geometry\FBXMeshGeometryHelper.h"
#include "FBXVertexCreator.h"

namespace wrapdll
{
    class FBXMeshCreator
    {
    public:
        static bool MakeUnindexPackedMesh(
            fbxsdk::FbxScene* poFbxScene,
            fbxsdk::FbxMesh* poFbxMesh,
            PackedMesh& destMesh,
            const std::vector<ControlPointInfluence>& controlPointerInfluences);
    private:
        static FbxVector4 GetTransformedNormal(
            FbxGeometryElementNormal::EMappingMode NormalMappingMode, 
            int controlPointIndex, 
            int vertexIndex, 
            const std::vector < fbxsdk::FbxVector4>& m_vecNormals, 
            FbxAMatrix transform);

        static FbxVector4 GetTransformedPosition(
            FbxVector4* pControlPoints, 
            int controlPointIndex, 
            FbxAMatrix& transform);
    };
}
