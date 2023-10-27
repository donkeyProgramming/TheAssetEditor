#pragma once

#include "..\DataStructures\SceneContainer\SceneContainer.h"

namespace wrapdll
{ 
    class IFbxMeshCreator
    {
    public:
        virtual fbxsdk::FbxMesh* Create(
            fbxsdk::FbxScene* poFbxScene,
            const PackedMesh& inMesh,
            SceneContainer& sceneContaner) = 0;
    protected:
        virtual bool InitFbxMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh) = 0;
        virtual bool SetNormalVectors(const PackedMesh& inMesh) = 0;
        virtual bool SetTextureCoords(const PackedMesh& inMesh) = 0;
        virtual bool SetPolygonFaces(const PackedMesh& inMesh) = 0;
        //virtual bool AddSkinning(
        //    fbxsdk::FbxMesh* poMesh,
        //    fbxsdk::FbxScene* poFbxScene,
        //    const PackedMesh& inMesh,
        //    SceneContainer& sceneContainer) = 0;
    protected:
         fbxsdk::FbxMesh* m_poFbxMesh;
    };
    
    class FbxMeshUnindexedCreator : public IFbxMeshCreator    
    {
    public:
         fbxsdk::FbxMesh* Create(
            fbxsdk::FbxScene* poFbxScene,
            const PackedMesh& inMesh,
            SceneContainer& sceneContaner);

    private:
        /// <summary>
        /// Makes an FbxFbx Node with an FbxMesh
        /// </summary>
        bool InitFbxMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh);

        /// <summary>
        /// Adds Postions
        /// </summary>    
        bool SetControlPoints(const PackedMesh& inMesh, double scaleFactor);

        /// <summary>
        /// Sets normals by triangle corner, so each triangle has 3 normals stored, 
        /// </summary>    
        bool SetNormalVectors(const PackedMesh& inMesh);

        /// <summary>
        ///  Set UVs per control point
        /// </summary>    
        bool SetTextureCoords(const PackedMesh& inMesh);

        /// <summary>
        /// Set the triangles faces of the mesh
        /// </summary>
        bool SetPolygonFaces(const PackedMesh& inMesh);

       /* bool AddSkinning(
            fbxsdk::FbxMesh* poMesh,
            fbxsdk::FbxScene* poFbxScene,
            const PackedMesh& inMesh,
            SceneContainer& sceneContainer);      */

    };                                                                                                                                                                   

};

