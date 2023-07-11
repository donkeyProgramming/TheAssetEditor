#pragma once
#include <fbxsdk.h>
#include <vector>
#include <map>

namespace wrapdll
{
	class FBXMeshGeometryHelper
	{
	public:
		/// <summary>
		/// Get UV maps into a string key map, for all files so far, the default key string is "diffuse"
		/// </summary>
		/// <param name="pMesh">FbxMesh</param>
		/// <returns>String keyes map of UV coords, key="diffuse" seems to be default</returns>
		static std::map<std::string, std::vector<fbxsdk::FbxVector2>> LoadUVInformation(fbxsdk::FbxMesh* pMesh);

		/// <summary>
		/// Gets normal vectors, supports all possible "FbxGeomtryElementNormal" storeage types/indexing types 		
		/// </summary>
		/// <param name="poMesh"></param>
		/// <param name="poMappingMode"></param>
		/// <returns></returns>
		static std::vector<FbxVector4> GetNormals(fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode = nullptr);
		static std::vector<FbxVector4> GetBitangents(fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode = nullptr);
		static std::vector<FbxVector4> GetTangents(fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode = nullptr);

	private:
		static std::vector<FbxVector4> FetchVectors(fbxsdk::FbxMesh* poMesh, fbxsdk::FbxLayerElementTemplate<fbxsdk::FbxVector4>* lNormalElement);


	};
}
