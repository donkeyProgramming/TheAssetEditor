#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"
#include "..\Logging\Logging.h"
#include "..\Helpers\Geometry\FBXNodeGeometryHelper.h"


namespace wrapdll
{
    class FbxMeshCreator
    {   

    public:
        static fbxsdk::FbxMesh* CreateFbxUnindexedMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh, double scaleFactor);

    private:
        /// <summary>
        /// Makes an FbxFbx Node with an FbxMesh
        /// </summary>
        static fbxsdk::FbxMesh* InitFbxMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh);

        /// <summary>
        /// Makes Unindexed Mesh
        /// </summary>    
        static bool SetControlPoints(fbxsdk::FbxMesh* poMesh, const PackedMesh& inMesh, double scaleFactor);

        /// <summary>
        /// Sets normals by triangle corner, so each triangle has 3 normals stored, 
        /// </summary>    
        static bool SetNormalVectors(fbxsdk::FbxMesh* poMesh, const PackedMesh& inMesh);

        /// <summary>
        ///  Set UVs per control point
        /// </summary>    
        static bool SetTextureCoords(fbxsdk::FbxMesh* poMesh, const PackedMesh& inMesh);

        /// <summary>
        /// Set the triangles faces of the mesh
        /// </summary>
        static bool SetPolygonFaces(fbxsdk::FbxMesh* poMesh, const PackedMesh& inMesh);
    };

};