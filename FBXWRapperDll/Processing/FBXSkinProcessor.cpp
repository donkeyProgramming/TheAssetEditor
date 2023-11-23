#include "FBXSkinProcessor.h"
#include "..\Logging\Logging.h"
#include "..\DataStructures\PackedMeshStructs.h"


using namespace wrapdll;

bool FBXSkinProcessorService::GetInfluencesFromNode(
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
    fbxsdk::FbxSkin* pFbxSkin = (FbxSkin*)_poSourceFbxMesh->GetDeformer(0);
    if (!pFbxSkin) 
    {
        LogActionWarning(std::string(_poSourceFbxMesh->GetName()) + "pFbxSkin == NULL, no Skin found!");
        return true;
    }

    return GetInfluencesFromSkin(pFbxSkin, _poSourceFbxMesh, controlPointInfluences);
}

bool FBXSkinProcessorService::GetInfluencesFromSkin(
    fbxsdk::FbxSkin* poSkin, 
    fbxsdk::FbxMesh* poFbxMesh,    
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
            LogActionError("NULL pointer for weight or indices");
            continue;
        }

        for (int influenceIndex = 0; influenceIndex < controlPointIndexCount; influenceIndex++)
        {
            // get control point 
            int controlPointIndex = pControlPointIndices[influenceIndex];

            // get weight associated with vertex
            double boneWeight = static_cast<float>(pControlPointWeights[influenceIndex]);            

            // TODO: clean-up once it works
            
            //auto currentWeightIndex = controlPointInfluences[controlPointIndex].weightCount;

            // set info, associated with the control point, that the MeshCreator can use to assign weighting to all vertices
            //FillVertexInfluence(controlPointInfluences[controlPointIndex].influences[currentWeightIndex], boneName, clusterIndex, boneWeight);

            //controlPointInfluences[controlPointIndex].weightCount++;

            AddControlPointInfluence(controlPointInfluences[controlPointIndex], boneName, clusterIndex, boneWeight);

            if (controlPointInfluences[controlPointIndex].influences.size() > 4)
            {
                __debugbreak();
                return false;
            }
        };        
    }

    CheckForErrorWeights(controlPointInfluences);

    return true;
}

// TOOD: remove?
//void wrapdll::FBXSkinProcessorService::FillVertexInfluence(VertexInfluence& influence, std::string& boneName, int clusterIndex, double boneWeight)
//{
//    CopyToFixedString(influence.boneName, boneName);
//    influence.boneIndex = clusterIndex;
//    influence.weight = static_cast<float>(boneWeight);    
//}

void wrapdll::FBXSkinProcessorService::AddControlPointInfluence(ControlPointInfluence& controlPointInfluence, std::string& boneName, int clusterIndex, double boneWeight)
{
    VertexInfluence newInfluence;

    CopyToFixedString(newInfluence.boneName, boneName);
    newInfluence.boneIndex = clusterIndex;    
    newInfluence.weight = static_cast<float>(boneWeight);

    controlPointInfluence.influences.push_back(newInfluence);    
}

bool wrapdll::FBXSkinProcessorService::CheckForErrorWeights(const std::vector<ControlPointInfluence>& controlPointInfluences)
{
    for (auto& controlPointInfluence : controlPointInfluences)
    {
        float totalWeight = 0.0f;
        for (auto& influence : controlPointInfluence.influences)
        {
            totalWeight += influence.weight;
        }

        if (totalWeight > 1.05f || totalWeight < 0.95f)
        {
            LogActionError("Weight sum NOT 1.0 for control point(vertex)!");
            return false;
        }
    }

    return true;
}
