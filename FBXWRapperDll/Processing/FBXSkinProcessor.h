#pragma once

#include <fbxsdk.h>
#include <vector>

struct ControlPointInfluence;
struct VertexInfluence;

namespace wrapdll
{
    class FBXSkinProcessorService
    {
    public:
        /// <summary>
        /// Process the weighting of an FBXMesh
        /// Retrives the FBXSkin (if any)
        /// Stored the weights per "control point" (="FBX SDK slang" for mathematical vertex {x,y,z,w})
        /// </summary>
        /// <param name="_poSourceFbxMesh">FBXMesh* to get FBXSkin* from</param>		
        /// <param name="controlPointInfluences">per "control point" weighting</param>
        /// <returns>true on sucess, false on fatal error</returns>
        static bool GetInfluencesFromNode(
            FbxMesh* poFbxNode,
            std::vector<ControlPointInfluence>& controlPointInfluences);

    private:
        /// <summary>
        /// Helper method that retrieves the weighting information, from an FBXSkin*
        /// </summary>
        /// <param name="poSkin">FBXSkin* source</param>
        /// <param name="poFbxMeshNode">FbxMesh* "owner" os the FXBSkin </param>
        /// <param name="controlPointInfluences">per "control point" weighting</param>
        /// <returns>true on sucess, false on fatal error</returns>
        static bool GetInfluencesFromSkin(
            fbxsdk::FbxSkin* poSkin,
            fbxsdk::FbxMesh* poFbxMeshNode,
            std::vector<ControlPointInfluence>& controlPointInfluences);

        // TOOD: remove?
        static void FillVertexInfluence(VertexInfluence& controlPointInfluences, std::string& boneName, int clusterIndex, double boneWeight);

        static void AddControlPointInfluence(ControlPointInfluence& controlPointInfluence, std::string& boneName, int clusterIndex, double boneWeight);

        // TODO: remove?
        static bool CheckForErrorWeights(const std::vector<ControlPointInfluence>& controlPointInfluences);

    };

}
