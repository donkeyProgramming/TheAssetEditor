#include "FBXSkinProcessor.h"

using namespace wrapdll;

bool FBXSkinProcessorService::ProcessSkin(
    FbxMesh* _poSourceFbxMesh,
    std::vector<ControlPointInfluence>& controlPointInfluences)
{
    LogAction("Processing Skin for mesh: " + std::string(_poSourceFbxMesh->GetName()));

    int deformerCount = _poSourceFbxMesh->GetDeformerCount();    
    if (deformerCount < 1) // -- no weighting data in mesh
    {
        LogActionWarning(std::string(_poSourceFbxMesh->GetName()) + ": no deformer/skin modifier found. No Rigging Will Be Added.");
        return true; 
    }

    // -- get skin 0, if there is more than one skin modifier (FbxSkin object), 
    // -- it is the users resposibility to make sure "skin 0" is the corect one, using their 3d modelling program
    fbxsdk::FbxSkin* pSkin = (FbxSkin*)_poSourceFbxMesh->GetDeformer(0);
    if (!pSkin) // no skin found
    {
        LogActionWarning(std::string(_poSourceFbxMesh->GetName()) + ":pSkin == NULL ");
        return true;
    }

    return GetInfluencesFromSkin(pSkin, _poSourceFbxMesh, controlPointInfluences);
}

bool FBXSkinProcessorService::GetInfluencesFromSkin(
    fbxsdk::FbxSkin*
    poSkin, fbxsdk::FbxMesh* poFbxMesh,    
    std::vector<ControlPointInfluence>& controlPointInfluences)
{
    // -- reset the control point influence container
    auto controlPointCount = poFbxMesh->GetControlPointsCount();
    controlPointInfluences.clear();
    controlPointInfluences.resize(controlPointCount);

    LogAction("Skin Name: " + std::string(poSkin->GetName()));

    int cluster_count = poSkin->GetClusterCount();

    // check if there is no rigging data for this skin
    if (cluster_count < 1)
    {
        return LogActionWarning(std::string(poFbxMesh->GetName()) + ": no weighting data found for skin: " + std::string(poSkin->GetName()) + " !");
    }

    // -- Rund through all clusters (1 cluster = 1 bone, kinda)
    LogAction("Processing: " + std::to_string(cluster_count) + " Skin Clusters...");

    for (int clusterIndex = 0; clusterIndex < cluster_count; clusterIndex++)
    {
        // Get the collection of clusters {vertex, weight}
        fbxsdk::FbxCluster* pCluster = poSkin->GetCluster(clusterIndex);

        // Get the "bone = the node which is affecting this FbxCluster
        fbxsdk::FbxNode* pBoneNode = pCluster->GetLink();
        std::string boneName = pBoneNode->GetName();

        int controlPointIndexCount = pCluster->GetControlPointIndicesCount();

        if (controlPointIndexCount < 1)
        {
            //log_action_warning("No Influences for bone: "+ strBoneName+ "");
            continue;
        }

        // get the indices and weights the current cluster (bone)
        int* pControlPointIndices = pCluster->GetControlPointIndices();
        double* pControlPointWeights = pCluster->GetControlPointWeights();

        if (!pControlPointIndices || !pControlPointWeights)
        {
            LogActionError("NULL pointer for weight/indices");
            continue;
        }

        for (int influenceIndex = 0; influenceIndex < controlPointIndexCount; influenceIndex++)
        {
            // get control point 
            int controlPointIndex = pControlPointIndices[influenceIndex];

            // get weight associated with vertex
            double boneWeight = pControlPointWeights[influenceIndex];

            controlPointInfluences[controlPointIndex].weightCount++;
            auto currentWeightIndex = controlPointInfluences[controlPointIndex].weightCount;

            // set info, associated with the control point, that the MeshCreator can use to assign weighting to all vertices
            CopyToFixedString(controlPointInfluences[controlPointIndex].influences[currentWeightIndex - 1].boneName, boneName);
            controlPointInfluences[controlPointIndex].influences[currentWeightIndex - 1].boneIndex = clusterIndex;
            controlPointInfluences[controlPointIndex].influences[currentWeightIndex - 1].weight = static_cast<float>(boneWeight);
        };
    }

    return true;
}