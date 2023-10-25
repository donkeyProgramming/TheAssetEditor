#pragma once
#include <fbxsdk.h>
#include <vector>
#include <map>
#include "..\..\DataStructures\PackedMeshStructs.h"

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
		/// <param name="poMesh">Source FbxMesh object</param>
		/// <param name="poMappingMode">fbxsdk mapping mode for vectors</param>
		/// <returns>an std::vector of FbxVector4, or an empty container on error</returns>
		static std::vector<FbxVector4> GetNormals(const fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode = nullptr);
		        
        /// <summary>
        /// Gets bitangent vectors, supports all possible "FbxGeomtryElementNormal" storeage types/indexing types 		
        /// </summary>
        /// <param name="poMesh">Source FbxMesh object</param>
        /// <param name="poMappingMode">fbxsdk mapping mode for vectors</param>
        /// <returns>an std::vector of FbxVector4, or an empty container on error</returns>
        static std::vector<FbxVector4> GetBitangents(const fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode = nullptr);
		
        /// <summary>
        /// Gets tangent vectors , supports all possible "FbxGeomtryElementNormal" storeage types/indexing types 		
        /// </summary>
        /// <param name="poMesh">Source FbxMesh object</param>
        /// <param name="poMappingMode">fbxsdk mapping mode for vectors</param>
        /// <returns>an std::vector of FbxVector4, or an empty container on error</returns>
        static std::vector<FbxVector4> GetTangents(const fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode = nullptr);

	private:
		/// <summary>
		/// Helper method that fecthes the vectors, and return a "EMappingMode*"                
		/// </summary>
		/// <param name="poMesh">source mesh</param>
		/// <param name="lNormalElement">EMappingMode enum value</param>
		/// <returns></returns>

		static std::vector<FbxVector4> FetchVectors(const fbxsdk::FbxMesh* poMesh, const FbxLayerElementTemplate<FbxVector4>* poNormalElement);


	};
}
