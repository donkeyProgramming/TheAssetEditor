#include "FBXSkinProcessor.h"

using namespace wrapdll;

/// <summary>
/// Processes FBXSkin, 
/// convert the riging to simple {boneIndex, weight}
/// add store for per "control point"
/// </summary>
/// <param name="_poSourceFbxMesh"></param>
/// <param name="destPackedMesh"></param>
/// <param name="boneTable"></param>
/// <param name="controlPointInfluences"></param>
/// <returns></returns>
bool FBXSkinProcessorService::ProcessSkin(
	FbxMesh* _poSourceFbxMesh, 
	PackedMesh& destPackedMesh, 
	const std::vector <std::string>& boneTable, 
	std::vector<ControlPointInfluences>& controlPointInfluences)
{	
	log_action("Processing Skin for mesh: "+ std::string(_poSourceFbxMesh->GetName()));	
	
	int deformerCount = _poSourceFbxMesh->GetDeformerCount();

	// Error checking,
	if (deformerCount < 1)
	{
		return log_action_warning(std::string(_poSourceFbxMesh->GetName()) + ": no deformer/skin modifier found. No Rigging Will Be Added.");
	}

	// get skin 0, if there are more than one skin modifier, it is the users resposibility to make sure that skin 0 is the  "mesh skin"
	fbxsdk::FbxSkin* pSkin = (FbxSkin*)_poSourceFbxMesh->GetDeformer(0);
	if (!pSkin)
	{
		return log_action_warning(std::string(_poSourceFbxMesh->GetName()) + ":pSkin == NULL ");
	}

	return GetInfluencesFromSkin(pSkin, _poSourceFbxMesh,  destPackedMesh, boneTable, controlPointInfluences);
}

bool FBXSkinProcessorService::GetInfluencesFromSkin(
	fbxsdk::FbxSkin* 
	poSkin, fbxsdk::FbxMesh* poFbxMesh, 
	PackedMesh& destPackedMesh, 
	const std::vector<std::string>& boneTable, 	
	std::vector<ControlPointInfluences>& controlPointInfluences 
)
{
	// -- reset the control point influence container
	auto controlPointCount = poFbxMesh->GetControlPointsCount();
	controlPointInfluences.clear();
	controlPointInfluences.resize(controlPointCount);


	log_action("Skin Name: " + std::string(poSkin->GetName()));

	int cluster_count = poSkin->GetClusterCount();

	// check if there is no rigging data for this skin
	if (cluster_count < 1)
	{
		return log_action_warning(std::string(poFbxMesh->GetName()) + ": no weighting data found for skin: " + std::string(poSkin->GetName()) + " !");
	}
	
	// -- Rund through all clusters (1 cluster = 1 bone, kinda)
	log_action("Processing: " + std::to_string(cluster_count) + " Skin Clusters...")
	for (int clusterIndex = 0; clusterIndex < cluster_count; clusterIndex++)
	{
		// Get the collection of clusters {vertex, weight}
		fbxsdk::FbxCluster* pCluster = poSkin->GetCluster(clusterIndex);

		// Get the "bone = the node which is affecting this FbxCluster
		fbxsdk::FbxNode* pBoneNode = pCluster->GetLink();

		// get the bone ID for this bone name
		std::string strBoneName = std::tolower(pBoneNode->GetName());
				
		log_action("Bone Table Size: " + std::to_string(boneTable.size()));

		int boneIndexValue = tools::GetIndexOf(strBoneName, boneTable);
		if (boneIndexValue == -1)
			return log_action_error("Bone in FBX File: '" + strBoneName + "' is not found in skeleton ANIM file!");

		log_action("Processing weighting for bone: '" + strBoneName + ", ID: " + std::to_string(boneIndexValue));

		// get the number of control point that this "bone" is affecting
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
			log_action_error("NULL pointer for weight/indices"); 
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

			controlPointInfluences[controlPointIndex].influences[currentWeightIndex - 1].boneIndex = boneIndexValue;
			controlPointInfluences[controlPointIndex].influences[currentWeightIndex - 1].weight = static_cast<float>(boneWeight);
		};
	}

	return true;
}

