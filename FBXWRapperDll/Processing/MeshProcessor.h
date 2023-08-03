/**
*
*   MeshProcessor.h
*
*   Does Tangentbasis calculation and indexing
*   Heavily inspired by the code in
*   https://github.com/huamulan/OpenGL-tutorial/blob/master/common/vboindexer.hpp
*   Which uses the WTFPL Public Licence
*
*/

#pragma once

#include <vector>
#include <map>
#include <string.h> 
#include <string> 

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"
#include "..\Logging\Logging.h"
#include "..\Helpers\Geometry\FBXNodeGeometryHelper.h"
#include "..\Helpers\Geometry\FBXMeshGeometryHelper.h"
#include "..\Helpers\MS_SimpleMath\SimpleMath.h"
#include "..\Helpers\VectorConverter.h"
#include "FBXVertexCreator.h"
#include "FBXMeshCreator.h"

namespace wrapdll
{
    class MeshProcessor
    {
        struct BoneName4
        {
            std::string n1, n2, n3, n4;
        };

    public:
        static void DoFinalMeshProcessing(PackedMesh& mesh)
        {
            ComputeTangentBasisUnindexed(mesh.vertices);
            DoMeshIndexingWithTangentSmoothing(mesh);
        }

        static void DoTangentAndIndexing(PackedMesh& destMesh) {

            using namespace std;
            using namespace DirectX;

            // -- input
            vector<DirectX::SimpleMath::Vector3> vertices;
            vector<DirectX::SimpleMath::Vector2> uvs;
            vector<DirectX::SimpleMath::Vector3> normals;
            vector<DirectX::SimpleMath::Vector3> tangents;
            vector<DirectX::SimpleMath::Vector3> bitangents;
            vector<XMFLOAT4> bone_weights;
            vector<XMUINT4> bone_indices;
            vector<BoneName4> bone_names;

            // -- output
            vector<DirectX::SimpleMath::Vector3> out_vertices;
            vector<DirectX::SimpleMath::Vector2> out_uvs;
            vector<DirectX::SimpleMath::Vector3> out_normals;
            vector<DirectX::SimpleMath::Vector3> out_tangents;
            vector<DirectX::SimpleMath::Vector3> out_bitangents;

            vector<DirectX::XMFLOAT4> out_bone_weights;
            vector<DirectX::XMUINT4> out_bone_indices;
            vector<BoneName4> out_bone_names;

            vector<uint16_t> out_indices;

            // fill the UN-INDEXED vertex data into vectors
            for (auto& v : destMesh.vertices)
            {
                //vertices.push_back(v.position);
                vertices.push_back({ v.position.x, v.position.y,v.position.z });
                uvs.push_back(v.uv);
                normals.push_back(v.normal);
                bone_indices.push_back({ v.influences[0].boneIndex, v.influences[1].boneIndex, v.influences[2].boneIndex, v.influences[3].boneIndex });
                bone_weights.push_back({ v.influences[0].weight, v.influences[1].weight, v.influences[2].weight, v.influences[3].weight });
                bone_names.push_back({ v.influences[0].boneName, v.influences[1].boneName, v.influences[2].boneName, v.influences[3].boneName });
            };

            ComputeTangentBasisForUnindexedMesh(
                // inputs
                vertices, uvs, normals,

                // outputs	
                tangents, bitangents
            );

            // do indexing (clean up mesh), and average tangents
            indexVBO_TBN_Slow(
                vertices, uvs, normals, tangents, bitangents, bone_weights, bone_indices, bone_names,

                out_indices,
                out_vertices,
                out_uvs,
                out_normals,
                out_tangents,
                out_bitangents,
                out_bone_weights,
                out_bone_indices,
                out_bone_names
            );

            destMesh.vertices.clear();
            destMesh.vertices.resize(out_vertices.size());

            // copy the processed mesh data back into the mesh
            for (size_t i = 0; i < out_vertices.size(); i++)
            {
                auto& v = destMesh.vertices[i];
                auto& v_src = destMesh.vertices[i];
                //v.position = out_vertices[i];
                v.position = XMFLOAT4(out_vertices[i].x, out_vertices[i].y, out_vertices[i].z, 0);

                out_normals[i].Normalize();
                v.normal = out_normals[i];

                out_tangents[i].Normalize();
                out_bitangents[i].Normalize();

                v.tangent = out_tangents[i];
                v.bitangent = out_bitangents[i];

                v.uv = out_uvs[i];

                v.influences[0].boneIndex = out_bone_indices[i].x;
                v.influences[1].boneIndex = out_bone_indices[i].y;
                v.influences[2].boneIndex = out_bone_indices[i].z;
                v.influences[3].boneIndex = out_bone_indices[i].w;

                v.influences[0].weight = out_bone_weights[i].x;
                v.influences[1].weight = out_bone_weights[i].y;
                v.influences[2].weight = out_bone_weights[i].z;
                v.influences[3].weight = out_bone_weights[i].w;

                strcpy_s<255>(v.influences[0].boneName, out_bone_names[i].n1.c_str());
                strcpy_s<255>(v.influences[1].boneName, out_bone_names[i].n2.c_str());
                strcpy_s<255>(v.influences[2].boneName, out_bone_names[i].n3.c_str());
                strcpy_s<255>(v.influences[3].boneName, out_bone_names[i].n4.c_str());
            }

            destMesh.indices = out_indices;
        }

        // TODO: "DoFinalMeshProcessing" does the same, REMOVE!
        static void DoTangentAndIndexingPacked(PackedMesh& destMesh)
        {   
            ComputeTangentBasisUnindexed(destMesh.vertices);

            std::vector<PackedCommonVertex> outVertices;
            std::vector<uint16_t> outIndices;
            // do indexing (clean up mesh), and average tangents
            // TODO: this is testing, set back old if doesn't work
            indexVBO_TBN_Slow(destMesh.vertices, outIndices, outVertices);
            //indexVBO_TBN_Fast_Packed(destMesh.vertices, outIndices, outVertices);
            
            destMesh.vertices = outVertices;            
            destMesh.indices = outIndices;
        }
        
        // TODO: "DoFinalMeshProcessing" does the same, REMOVE!
        static void DoMeshIndexingWithTangentSmoothing(PackedMesh& destMesh)
        {             
            std::vector<PackedCommonVertex> outVertices;
            std::vector<uint16_t> outIndices;            
            
            // TODO: this is testing, set back old if doesn't work
            indexVBO_TBN_Slow(destMesh.vertices, outIndices, outVertices);
            //indexVBO_TBN_Fast_Packed(destMesh.vertices, outIndices, outVertices);            

            destMesh.vertices = outVertices;            
            destMesh.indices = outIndices;
        }

        static void ComputeTangentBasisForUnindexedMesh(
            // inputs
            const std::vector<DirectX::SimpleMath::Vector3>& vertices,
            const std::vector<DirectX::SimpleMath::Vector2>& uvs,
            const std::vector<DirectX::SimpleMath::Vector3>& normals,
            // outputs
            std::vector<DirectX::SimpleMath::Vector3>& tangents,
            std::vector<DirectX::SimpleMath::Vector3>& bitangents
        ) 
        {
            for (unsigned int i = 0; i < vertices.size(); i += 3) {
                // Shortcuts for vertices
                const DirectX::SimpleMath::Vector3& v0 = vertices[i + 0];
                const DirectX::SimpleMath::Vector3& v1 = vertices[i + 1];
                const DirectX::SimpleMath::Vector3& v2 = vertices[i + 2];

                // Shortcuts for UVs
                const DirectX::SimpleMath::Vector2& uv0 = uvs[i + 0];
                const DirectX::SimpleMath::Vector2& uv1 = uvs[i + 1];
                const DirectX::SimpleMath::Vector2& uv2 = uvs[i + 2];

                // Edges of the triangle : postion delta
                DirectX::SimpleMath::Vector3 deltaPos1 = v1 - v0;
                DirectX::SimpleMath::Vector3 deltaPos2 = v2 - v0;

                // UV delta
                DirectX::SimpleMath::Vector2 deltaUV1 = uv1 - uv0;
                DirectX::SimpleMath::Vector2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
                DirectX::SimpleMath::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
                DirectX::SimpleMath::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

                // tangent/bitangent will be averages during indexing
                tangents.push_back(tangent);
                tangents.push_back(tangent);
                tangents.push_back(tangent);

                bitangents.push_back(bitangent);
                bitangents.push_back(bitangent);
                bitangents.push_back(bitangent);
            }

            return;
        };

        // TODO: CLEAN UP
        static void ComputeTangentBasisUnindexed(std::vector<PackedCommonVertex>& vertices)
        {
            for (size_t i = 0; i < vertices.size(); i += 3) 
            {
                // Shortcuts for vertices
                const DirectX::SimpleMath::Vector3& v0 = convert::ConvertToVec3(vertices[i + 0u].position);
                const DirectX::SimpleMath::Vector3& v1 = convert::ConvertToVec3(vertices[i + 1u].position);
                const DirectX::SimpleMath::Vector3& v2 = convert::ConvertToVec3(vertices[i + 2u].position);

                // Shortcuts for UVs
                const DirectX::SimpleMath::Vector2& uv0 = vertices[i + 0u].uv;
                const DirectX::SimpleMath::Vector2& uv1 = vertices[i + 1u].uv;
                const DirectX::SimpleMath::Vector2& uv2 = vertices[i + 2u].uv;

                // Edges of the triangle : postion delta
                DirectX::SimpleMath::Vector3 deltaPos1 = v1 - v0;
                DirectX::SimpleMath::Vector3 deltaPos2 = v2 - v0;

                // UV delta
                DirectX::SimpleMath::Vector2 deltaUV1 = uv1 - uv0;
                DirectX::SimpleMath::Vector2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
                DirectX::SimpleMath::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
                DirectX::SimpleMath::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

                vertices[i + 0u].tangent = tangent;
                vertices[i + 1u].tangent = tangent;
                vertices[i + 2u].tangent = tangent;

                vertices[i + 0u].bitangent = bitangent;
                vertices[i + 1u].bitangent = bitangent;
                vertices[i + 2u].bitangent = bitangent;                
            }            
        };

        // TODO: FINISH
        static void ComputeTangentBasisIndexed(std::vector<PackedCommonVertex>& vertices, const std::vector<uint16_t>& indices)
        {         
            // iterate over triangles
            for (size_t faceIndex = 0; faceIndex < indices.size(); faceIndex += 3)
            {                
                // Corner index-to-vertices of triangle N
                const auto& cornerIndex0 = indices[faceIndex + 0U];
                const auto& cornerIndex1 = indices[faceIndex + 1U];
                const auto& cornerIndex2 = indices[faceIndex + 2U];

                const DirectX::SimpleMath::Vector3& v0 = convert::ConvertToVec3(vertices[cornerIndex0].position);
                const DirectX::SimpleMath::Vector3& v1 = convert::ConvertToVec3(vertices[cornerIndex1].position);
                const DirectX::SimpleMath::Vector3& v2 = convert::ConvertToVec3(vertices[cornerIndex2].position);

                // Shortcuts for UVs
                const DirectX::SimpleMath::Vector2& uv0 = vertices[cornerIndex0].uv;
                const DirectX::SimpleMath::Vector2& uv1 = vertices[cornerIndex1].uv;
                const DirectX::SimpleMath::Vector2& uv2 = vertices[cornerIndex2].uv;

                // Edges of the triangle : postion delta
                DirectX::SimpleMath::Vector3 deltaPos1 = v1 - v0;
                DirectX::SimpleMath::Vector3 deltaPos2 = v2 - v0;

                // UV delta
                DirectX::SimpleMath::Vector2 deltaUV1 = uv1 - uv0;
                DirectX::SimpleMath::Vector2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
                DirectX::SimpleMath::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
                DirectX::SimpleMath::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

                vertices[cornerIndex0].tangent = tangent;
                vertices[cornerIndex1].tangent = tangent;
                vertices[cornerIndex2].tangent = tangent;

                vertices[cornerIndex0].bitangent = bitangent;
                vertices[cornerIndex1].bitangent = bitangent;
                vertices[cornerIndex2].bitangent = bitangent;
            }
        }

        static inline void indexVBO_TBN_Slow(
            const std::vector<DirectX::SimpleMath::Vector3>& in_vertices,
            const std::vector<DirectX::SimpleMath::Vector2>& in_uvs,
            const std::vector<DirectX::SimpleMath::Vector3>& in_normals,
            const std::vector<DirectX::SimpleMath::Vector3>& in_tangents,
            const std::vector<DirectX::SimpleMath::Vector3>& in_bitangents,

            const std::vector<DirectX::XMFLOAT4>& in_bone_weights,
            const std::vector<DirectX::XMUINT4>& in_bone_indices,
            const std::vector<BoneName4>& in_bone_names,

            //std::vector<uint16_t>& out_vertex_remap,
            std::vector<uint16_t>& out_indices,
            std::vector<DirectX::SimpleMath::Vector3>& out_vertices,
            std::vector<DirectX::SimpleMath::Vector2>& out_uvs,
            std::vector<DirectX::SimpleMath::Vector3>& out_normals,
            std::vector<DirectX::SimpleMath::Vector3>& out_tangents,
            std::vector<DirectX::SimpleMath::Vector3>& out_bitangents,

            std::vector<DirectX::XMFLOAT4>& out_bone_weights,
            std::vector<DirectX::XMUINT4>& out_bone_indices,
            std::vector<BoneName4>& out_bone_names
        ) {
            //std::map<PackedCommonVertex, unsigned short> VertexToOutIndex;

            std::vector<int> avg_count/*(in_vertices.size(), 1)*/;
            // For each input vertex
            for (unsigned int i = 0; i < in_vertices.size(); i++) {
                PackedCommonVertex packed;
                packed.position = convert::ConvertToVec3(in_vertices[i]);
                packed.uv = in_uvs[i];
                packed.normal = in_normals[i];

                // Try to find a similar vertex in out_XXXX
                uint16_t index;

                //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
                bool found = getSimilarVertexIndex(in_vertices[i], in_uvs[i], in_normals[i], out_vertices, out_uvs, out_normals, index);

                if (found) { // A similar vertex is already in the VBO, use it instead !
                    out_indices.push_back(index);

                    // Average the tangents and the bitangents
                    out_tangents[index] += in_tangents[i];
                    out_bitangents[index] += in_bitangents[i];

                    //avg_count[index]++;
                }
                else { // If not, it needs to be added in the output data.
                    out_vertices.push_back(in_vertices[i]);
                    out_uvs.push_back(in_uvs[i]);
                    out_normals.push_back(in_normals[i]);
                    out_tangents.push_back(in_tangents[i]);
                    out_bitangents.push_back(in_bitangents[i]);

                    out_bone_weights.push_back(in_bone_weights[i]);
                    out_bone_indices.push_back(in_bone_indices[i]);
                    out_bone_names.push_back(in_bone_names[i]);

                    uint16_t newindex = (uint16_t)out_vertices.size() - 1;

                    out_indices.push_back(newindex);
                    //VertexToOutIndex[packed] = newindex;

                    //avg_count.push_back(1);
                    // the index in the INPUT, for remapping
                    //out_vertex_remap.push_back(i);
                }
            }
        }

        static inline void indexVBO_TBN_Slow(
            const std::vector<PackedCommonVertex>& in_vertices,            
            std::vector<uint16_t>& out_indices,
            std::vector<PackedCommonVertex>& out_vertices
        ) {
            //std::map<PackedCommonVertex, unsigned short> VertexToOutIndex;

            std::vector<int> avg_count/*(in_vertices.size(), 1)*/;
            // For each input vertex
            for (unsigned int i = 0; i < in_vertices.size(); i++) {
                PackedCommonVertex packed;
                packed.position = convert::ConvertToVec3(in_vertices[i].position);
                packed.uv = in_vertices[i].uv;
                packed.normal = in_vertices[i].normal;

                // Try to find a similar vertex in out_XXXX
                uint16_t index;

                //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
                bool found = getSimilarVertexIndex(convert::ConvertToVec3(in_vertices[i].position), in_vertices[i].uv, in_vertices[i].normal, out_vertices, index);

                if (found) { // A similar vertex is already in the VBO, use it instead !
                    out_indices.push_back(index);

                    // Average the tangents and the bitangents
                    out_vertices[index].tangent = sm::Vector3(out_vertices[index].tangent) + sm::Vector3(in_vertices[i].tangent);
                    out_vertices[index].bitangent = sm::Vector3(out_vertices[index].bitangent) + sm::Vector3(in_vertices[i].bitangent);


                    //avg_count[index]++;
                }
                else { // If not, it needs to be added in the output data.
                    out_vertices.push_back(in_vertices[i]);

                    uint16_t newindex = (uint16_t)out_vertices.size() - 1;

                    out_indices.push_back(newindex);
                    //VertexToOutIndex[packed] = newindex;

                    //avg_count.push_back(1);
                    // the index in the INPUT, for remapping
                    //out_vertex_remap.push_back(i);
                }
            }
        }
        struct PackedVertex {
            sm::Vector3 position;
            sm::Vector2 uv;
            sm::Vector3 normal;
            bool operator<(const PackedVertex that) const {
                return memcmp((void*)this, (void*)&that, sizeof(PackedVertex)) > 0;
            };
        };

        static bool getSimilarVertexIndex_fast(
            PackedVertex& packed,
            std::map<PackedVertex, unsigned short>& VertexToOutIndex,
            unsigned short& result
        ) {
            std::map<PackedVertex, unsigned short>::iterator it = VertexToOutIndex.find(packed);
            if (it == VertexToOutIndex.end()) {
                return false;
            }
            else {
                result = it->second;
                return true;
            }
        }
        static inline void indexVBO_TBN_Fast_Packed(
            const std::vector<PackedCommonVertex>& in_vertices,            
            std::vector<uint16_t>& out_indices,
            std::vector<PackedCommonVertex>& out_vertices
        ) {            
            std::map<PackedVertex, unsigned short> VertexToOutIndex;
                       
            // For each input vertex
            for (unsigned int i = 0; i < in_vertices.size(); i++) 
            {
                PackedVertex packedVertex;
                packedVertex.position = convert::ConvertToVec3(in_vertices[i].position);
                packedVertex.uv = in_vertices[i].uv;
                packedVertex.normal = in_vertices[i].normal;

                // Try to find a similar vertex in out_XXXX
                uint16_t index;

                //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
                bool found = getSimilarVertexIndex_fast(packedVertex, VertexToOutIndex, index);

                if (found) { // A similar vertex is already in the VBO, use it instead !
                    out_indices.push_back(index);

                    // Average the tangents and the bitangents
                    out_vertices[index].tangent = sm::Vector3(out_vertices[index].tangent) + sm::Vector3(in_vertices[i].tangent);
                    out_vertices[index].bitangent = sm::Vector3(out_vertices[index].bitangent) + sm::Vector3(in_vertices[i].bitangent);
                }
                else 
                { // If not, it needs to be added in the output data.
                    out_vertices.push_back(in_vertices[i]);

                    uint16_t newindex = (uint16_t)out_vertices.size() - 1;

                    out_indices.push_back(newindex);
                    VertexToOutIndex[packedVertex] = newindex;
                }
            }
        }


        static inline bool is_near(float v1, float v2) {
            return fabs(v1 - v2) < 0.00001f;
        }

        // Searches through all already-exported vertices
        // for a similar one.
        // Similar = same position + same UVs + same normal
        static inline bool getSimilarVertexIndex(
            const DirectX::SimpleMath::Vector3& in_vertex,
            const DirectX::SimpleMath::Vector2& in_uv,
            const DirectX::SimpleMath::Vector3& in_normal,
            std::vector<DirectX::SimpleMath::Vector3>& out_vertices,
            std::vector<DirectX::SimpleMath::Vector2>& out_uvs,
            std::vector<DirectX::SimpleMath::Vector3>& out_normals,
            uint16_t& result
        ) {
            // Lame linear search
            for (unsigned int i = 0; i < out_vertices.size(); i++) {
                if (
                    is_near(in_vertex.x, out_vertices[i].x) &&
                    is_near(in_vertex.y, out_vertices[i].y) &&
                    is_near(in_vertex.z, out_vertices[i].z) &&
                    is_near(in_uv.x, out_uvs[i].x) &&
                    is_near(in_uv.y, out_uvs[i].y) &&
                    is_near(in_normal.x, out_normals[i].x) &&
                    is_near(in_normal.y, out_normals[i].y) &&
                    is_near(in_normal.z, out_normals[i].z)
                    ) {
                    result = i;
                    return true;
                }
            }
            // No other vertex could be used instead.
            // Looks like we'll have to add it to the VBO.
            return false;
        }

        static inline bool getSimilarVertexIndex(
            const DirectX::SimpleMath::Vector3& in_vertex,
            const DirectX::SimpleMath::Vector2& in_uv,
            const DirectX::SimpleMath::Vector3& in_normal,
            std::vector<PackedCommonVertex>& out_vertices,

            uint16_t& result
        ) {
            // Lame linear search
            for (unsigned int i = 0; i < out_vertices.size(); i++) {
                if (
                    is_near(in_vertex.x, out_vertices[i].position.x) &&
                    is_near(in_vertex.y, out_vertices[i].position.y) &&
                    is_near(in_vertex.z, out_vertices[i].position.z) &&
                    is_near(in_uv.x, out_vertices[i].uv.x) &&
                    is_near(in_uv.y, out_vertices[i].uv.y) &&
                    is_near(in_normal.x, out_vertices[i].normal.x) &&
                    is_near(in_normal.y, out_vertices[i].normal.y) &&
                    is_near(in_normal.z, out_vertices[i].normal.z)
                    ) {
                    result = i;
                    return true;
                }
            }
            // No other vertex could be used instead.
            // Looks like we'll have to add it to the VBO.
            return false;
        }


    public:

        void indexVBO_TBN(
            std::vector<sm::Vector3>& in_vertices,
            std::vector<sm::Vector2>& in_uvs,
            std::vector<sm::Vector3>& in_normals,
            std::vector<sm::Vector3>& in_tangents,
            std::vector<sm::Vector3>& in_bitangents,

            std::vector<unsigned short>& out_indices,
            std::vector<sm::Vector3>& out_vertices,
            std::vector<sm::Vector2>& out_uvs,
            std::vector<sm::Vector3>& out_normals,
            std::vector<sm::Vector3>& out_tangents,
            std::vector<sm::Vector3>& out_bitangents
        ) {
            // For each input vertex
            for (unsigned int i = 0; i < in_vertices.size(); i++) {

                // Try to find a similar vertex in out_XXXX
                unsigned short index;
                bool found = getSimilarVertexIndex(in_vertices[i], in_uvs[i], in_normals[i], out_vertices, out_uvs, out_normals, index);

                if (found) { // A similar vertex is already in the VBO, use it instead !
                    out_indices.push_back(index);

                    // Average the tangents and the bitangents
                    out_tangents[index] += in_tangents[i];
                    out_bitangents[index] += in_bitangents[i];
                }
                else { // If not, it needs to be added in the output data.
                    out_vertices.push_back(in_vertices[i]);
                    out_uvs.push_back(in_uvs[i]);
                    out_normals.push_back(in_normals[i]);
                    out_tangents.push_back(in_tangents[i]);
                    out_bitangents.push_back(in_bitangents[i]);
                    out_indices.push_back((unsigned short)out_vertices.size() - 1);
                }
            }
        }
      


        struct PackedVertexExt {

        private:
            DirectX::PackedVector::HALF posX;
            DirectX::PackedVector::HALF posY;
            DirectX::PackedVector::HALF posZ;

            DirectX::PackedVector::HALF uvX;
            DirectX::PackedVector::HALF uvY;

            uint8_t normX;
            uint8_t normY;
            uint8_t normZ;

        public:
            PackedVertexExt
            (sm::Vector3 position,
                sm::Vector2 uv,
                sm::Vector3 normal)



            {

                using namespace DirectX;
                posX = DirectX::PackedVector::XMConvertFloatToHalf(position.x);
                posY = DirectX::PackedVector::XMConvertFloatToHalf(position.y);
                posZ = DirectX::PackedVector::XMConvertFloatToHalf(position.z);

                uvX = DirectX::PackedVector::XMConvertFloatToHalf(uv.x);
                uvY = DirectX::PackedVector::XMConvertFloatToHalf(uv.y);

                // TODO: finsih? or throw out?


            }
            bool operator<(const PackedVertex that) const
            {
                return memcmp((void*)this, (void*)&that, sizeof(PackedVertex)) > 0;
            };
        };
    };
};