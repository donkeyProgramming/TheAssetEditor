//#pragma once
//
//#include <fbxsdk.h>
//
//#include <vector>
//#include <map>
//
//#include "..\DataStructures\PackedMeshStructs.h"
//#include "..\Helpers\FBXUnitHelper.h"
//#include "..\Logging\Logging.h"
//#include "..\Helpers\Geometry\FBXNodeGeometryHelper.h"
//#include "..\Helpers\Geometry\FBXMeshGeometryHelper.h"
//#include "..\Helpers\MS_SimpleMath\SimpleMath.h"
//#include "..\Helpers\VectorConverter.h"
//#include "PackedVertexCreator.h"
//#include "FBXMeshCreator.h"
//
//namespace wrapdll
//{
//	class FbxMeshProcessor
//	{
//
//	public:
//		static void doTangentAndIndexing(PackedMesh& destMesh) {
//
//			using namespace std;
//			using namespace DirectX;
//
//			vector<DirectX::SimpleMath::Vector3> vertices;
//			vector<DirectX::SimpleMath::Vector2> uvs;
//			vector<DirectX::SimpleMath::Vector3> normals;
//			vector<DirectX::SimpleMath::Vector3> tangents;
//			vector<DirectX::SimpleMath::Vector3> bitangents;
//
//			vector<XMFLOAT4> bone_weights;
//			vector<XMUINT4> bone_indices;
//
//			vector<DirectX::SimpleMath::Vector3> out_vertices;
//			vector<DirectX::SimpleMath::Vector2> out_uvs;
//			vector<DirectX::SimpleMath::Vector3> out_normals;
//			vector<DirectX::SimpleMath::Vector3> out_tangents;
//			vector<DirectX::SimpleMath::Vector3> out_bitangents;
//
//			vector<DirectX::XMFLOAT4> out_bone_weights;
//			vector<DirectX::XMUINT4> out_bone_indices;
//
//			vector<uint16_t> out_indices;
//
//			// fill the UN-INDEXED vertex data into vectors
//			for (auto& v : destMesh.vertices)
//			{
//				//vertices.push_back(v.position);
//				vertices.push_back({ v.position.x, v.position.y,v.position.z });
//				uvs.push_back(v.uv);
//				normals.push_back(v.normal);
//				bone_indices.push_back({ v.influences[0].boneIndex, v.influences[1].boneIndex, v.influences[2].boneIndex, v.influences[3].boneIndex });
//				bone_weights.push_back({ v.influences[0].weight, v.influences[1].weight, v.influences[2].weight, v.influences[3].weight });
//			};
//
//			computeTangentBasis_Unindexed(
//				// inputs
//				vertices, uvs, normals,
//
//				// outputs	
//				tangents, bitangents
//			);
//
//			// do indexing (clean up mesh), and average tangents
//			indexVBO_TBN_Fast(
//				vertices, uvs, normals, tangents, bitangents, bone_weights, bone_indices,
//				
//				out_indices,
//				out_vertices,
//				out_uvs,
//				out_normals,
//				out_tangents,
//				out_bitangents,				
//				out_bone_weights,
//				out_bone_indices
//			);
//			
//			destMesh.vertices.clear();
//			destMesh.vertices.resize(out_vertices.size());
//
//			// copy the processed mesh data back into the mesh
//			for (size_t i = 0; i < out_vertices.size(); i++)
//			{
//				auto& v = destMesh.vertices[i];
//				auto& v_src = destMesh.vertices[i];
//				//v.position = out_vertices[i];
//				v.position = XMFLOAT4(out_vertices[i].x, out_vertices[i].y, out_vertices[i].z, 0);
//
//				out_normals[i].Normalize();
//				v.normal = out_normals[i];
//
//				out_tangents[i].Normalize();
//				out_bitangents[i].Normalize();
//
//				v.tangent = out_tangents[i];
//				v.bitangent = out_bitangents[i];
//
//				v.uv = out_uvs[i];
//
//				v.influences[0].boneIndex = out_bone_indices[i].x;
//				v.influences[1].boneIndex = out_bone_indices[i].y;
//				v.influences[2].boneIndex = out_bone_indices[i].z;
//				v.influences[3].boneIndex = out_bone_indices[i].w;
//
//				v.influences[0].weight = out_bone_weights[i].x;
//				v.influences[1].weight = out_bone_weights[i].y;
//				v.influences[2].weight = out_bone_weights[i].z;
//				v.influences[3].weight = out_bone_weights[i].w;
//			}
//			
//			destMesh.indices = out_indices;
//		}
//
//		static void computeTangentBasis_Unindexed(
//			// inputs
//			const std::vector<DirectX::SimpleMath::Vector3>& vertices,
//			const std::vector<DirectX::SimpleMath::Vector2>& uvs,
//			const std::vector<DirectX::SimpleMath::Vector3>& normals,
//			// outputs
//			std::vector<DirectX::SimpleMath::Vector3>& tangents,
//			std::vector<DirectX::SimpleMath::Vector3>& bitangents
//		) {
//			for (unsigned int i = 0; i < vertices.size(); i += 3) {
//				// Shortcuts for vertices
//				const DirectX::SimpleMath::Vector3& v0 = vertices[i + 0];
//				const DirectX::SimpleMath::Vector3& v1 = vertices[i + 1];
//				const DirectX::SimpleMath::Vector3& v2 = vertices[i + 2];
//
//				// Shortcuts for UVs
//				const DirectX::SimpleMath::Vector2& uv0 = uvs[i + 0];
//				const DirectX::SimpleMath::Vector2& uv1 = uvs[i + 1];
//				const DirectX::SimpleMath::Vector2& uv2 = uvs[i + 2];
//
//				// Edges of the triangle : postion delta
//				DirectX::SimpleMath::Vector3 deltaPos1 = v1 - v0;
//				DirectX::SimpleMath::Vector3 deltaPos2 = v2 - v0;
//
//				// UV delta
//				DirectX::SimpleMath::Vector2 deltaUV1 = uv1 - uv0;
//				DirectX::SimpleMath::Vector2 deltaUV2 = uv2 - uv0;
//
//				float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
//				DirectX::SimpleMath::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
//				DirectX::SimpleMath::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;
//
//				// tangent/bitangent will be averages during indexing
//				tangents.push_back(tangent);
//				tangents.push_back(tangent);
//				tangents.push_back(tangent);
//				
//				bitangents.push_back(bitangent);
//				bitangents.push_back(bitangent);
//				bitangents.push_back(bitangent);
//			}
//
//			return;
//		};
//
//		static inline void indexVBO_TBN_Fast(
//			const std::vector<DirectX::SimpleMath::Vector3>& in_vertices,
//			const std::vector<DirectX::SimpleMath::Vector2>& in_uvs,
//			const std::vector<DirectX::SimpleMath::Vector3>& in_normals,
//			const std::vector<DirectX::SimpleMath::Vector3>& in_tangents,
//			const std::vector<DirectX::SimpleMath::Vector3>& in_bitangents,
//
//			const std::vector<DirectX::XMFLOAT4>& in_bone_weights,
//			const std::vector<DirectX::XMUINT4>& in_bone_indices,
//
//			//std::vector<uint16_t>& out_vertex_remap,
//			std::vector<uint16_t>& out_indices,
//			std::vector<DirectX::SimpleMath::Vector3>& out_vertices,
//			std::vector<DirectX::SimpleMath::Vector2>& out_uvs,
//			std::vector<DirectX::SimpleMath::Vector3>& out_normals,
//			std::vector<DirectX::SimpleMath::Vector3>& out_tangents,
//			std::vector<DirectX::SimpleMath::Vector3>& out_bitangents,
//
//			std::vector<DirectX::XMFLOAT4>& out_bone_weights,
//			std::vector<DirectX::XMUINT4>& out_bone_indices
//
//		) {
//			//std::map<PackedCommonVertex, unsigned short> VertexToOutIndex;
//
//			std::vector<int> avg_count/*(in_vertices.size(), 1)*/;
//			// For each input vertex
//			for (unsigned int i = 0; i < in_vertices.size(); i++) {
//				PackedCommonVertex packed;
//				packed.position = convert::ConvertToVec3(in_vertices[i]);
//				packed.uv = in_uvs[i];
//				packed.normal = in_normals[i];
//
//				// Try to find a similar vertex in out_XXXX
//				uint16_t index;
//
//				//bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
//				bool found = getSimilarVertexIndex(in_vertices[i], in_uvs[i], in_normals[i], out_vertices, out_uvs, out_normals, index);
//
//				if (found) { // A similar vertex is already in the VBO, use it instead !
//					out_indices.push_back(index);
//
//					// Average the tangents and the bitangents
//					out_tangents[index] += in_tangents[i];
//					out_bitangents[index] += in_bitangents[i];
//
//					//avg_count[index]++;
//				}
//				else { // If not, it needs to be added in the output data.
//					out_vertices.push_back(in_vertices[i]);
//					out_uvs.push_back(in_uvs[i]);
//					out_normals.push_back(in_normals[i]);
//					out_tangents.push_back(in_tangents[i]);
//					out_bitangents.push_back(in_bitangents[i]);
//
//					out_bone_weights.push_back(in_bone_weights[i]);
//					out_bone_indices.push_back(in_bone_indices[i]);
//
//					uint16_t newindex = (uint16_t)out_vertices.size() - 1;
//
//					out_indices.push_back(newindex);
//					//VertexToOutIndex[packed] = newindex;
//
//					//avg_count.push_back(1);
//					// the index in the INPUT, for remapping
//					//out_vertex_remap.push_back(i);
//				}
//			}
//		}
//
//		static inline bool is_near(float v1, float v2) {
//			return fabs(v1 - v2) < 0.00001f;
//		}
//
//		// Searches through all already-exported vertices
//		// for a similar one.
//		// Similar = same position + same UVs + same normal
//		static inline bool getSimilarVertexIndex(
//			const DirectX::SimpleMath::Vector3& in_vertex,
//			const DirectX::SimpleMath::Vector2& in_uv,
//			const DirectX::SimpleMath::Vector3& in_normal,
//			std::vector<DirectX::SimpleMath::Vector3>& out_vertices,
//			std::vector<DirectX::SimpleMath::Vector2>& out_uvs,
//			std::vector<DirectX::SimpleMath::Vector3>& out_normals,
//			uint16_t& result
//		) {
//			// Lame linear search
//			for (unsigned int i = 0; i < out_vertices.size(); i++) {
//				if (
//					is_near(in_vertex.x, out_vertices[i].x) &&
//					is_near(in_vertex.y, out_vertices[i].y) &&
//					is_near(in_vertex.z, out_vertices[i].z) &&
//					is_near(in_uv.x, out_uvs[i].x) &&
//					is_near(in_uv.y, out_uvs[i].y) &&
//					is_near(in_normal.x, out_normals[i].x) &&
//					is_near(in_normal.y, out_normals[i].y) &&
//					is_near(in_normal.z, out_normals[i].z)
//					) {
//					result = i;
//					return true;
//				}
//			}
//			// No other vertex could be used instead.
//			// Looks like we'll have to add it to the VBO.
//			return false;
//		}
//
//
//	public:
//
//
//	};
//};