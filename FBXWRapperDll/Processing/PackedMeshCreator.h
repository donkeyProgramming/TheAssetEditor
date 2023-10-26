#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\HelperUtils\FBXUnitHelper.h"
#include "..\Logging\Logging.h"

#include "..\HelperUtils\Geometry\FBXNodeGeometryHelper.h"
#include "..\HelperUtils\Geometry\FBXMeshGeometryHelper.h"

#include "PackedVertexCreator.h"

namespace wrapdll
{
    class PackedMeshCreator
    {
    public:
        /// <summary>
        /// Make (unindexed) "PackedMesh" from an "FbxMesh*"
        /// </summary>        
        /// <returns></returns>
        static bool MakeUnindexedPackedMesh (
            fbxsdk::FbxScene* poFbxScene,
            fbxsdk::FbxMesh* poFbxMesh,
            PackedMesh& destMesh,
            const std::vector<ControlPointInfluence>& controlPointerInfluences);

        // TODO: implement/remove/ 
        ///// <summary>
        ///// Make an (unindexed) "FbxMesh*" from "PackedMesh"
        ///// </summary>
        ///// <param name="sourcePackedMesh"></param>
        ///// <param name="poFbxScene"></param>
        ///// <param name="poDestFbxMesh"></param>
        ///// <returns></returns>
        //static bool MakeUnindexedFbxMesh(
        //    PackedMesh& sourcePackedMesh,
        //    fbxsdk::FbxScene* poFbxScene,
        //    fbxsdk::FbxMesh* poDestFbxMesh);

    private:
        static FbxVector4 GetFbxTransformedNormal(
            FbxGeometryElementNormal::EMappingMode NormalMappingMode, 
            int controlPointIndex, 
            int vertexIndex, 
            const std::vector < fbxsdk::FbxVector4>& m_vecNormals, 
            FbxAMatrix transform);

        static FbxVector4 GetFbxTransformedPosition(
            FbxVector4* pControlPoints, 
            int controlPointIndex, 
            FbxAMatrix& transform);
      
    };
}
