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

#include <SimpleMath.h>
#include "..\Logging\Logging.h"
#include "..\DataStructures\PackedMeshStructs.h"
#include "..\HelperUtils\VectorConverter.h"

namespace wrapdll
{
    class MeshProcessor
    {     
    private:
        static constexpr int VERTEX_DISCARDED = -1;

    public: 
        static void DoFinalMeshProcessing(PackedMesh& mesh);

        static void DoTangentBasisAndIndexing(PackedMesh& destMesh);

        static void RemapVertexWeights(const std::vector<VertexWeight>& inVertexWeights, std::vector<VertexWeight>& outVertexWeights, const std::vector<int>& outVertexIndexRemap);
        // TODO: make make the result the RETURN VALUE, and the input "srcMesh"?
        static void DoMeshIndexingWithTangentSmoothing(PackedMesh& destMesh)
        {
            std::vector<PackedCommonVertex> outVertices;
            std::vector<uint32_t> outIndices;
            std::vector<int> outVertexIndexRemap; // index = old index, value = new value            

            // TODO: Decide Which version is ACTUALLY faster??
            // TODO: Bade Name! Sacrifice speed: Split this up in several methods(loops)?
            DoMeshIndexingWithTangenSmoothing_OutPutRemap_Fast(destMesh.vertices, outVertices, outIndices, outVertexIndexRemap);

            std::vector<VertexWeight> outVertexWeights;
            RemapVertexWeights(destMesh.vertexWeights, outVertexWeights, outVertexIndexRemap);

            destMesh.vertices = outVertices;
            destMesh.indices = outIndices;
            destMesh.vertexWeights = outVertexWeights;

            // TODO: debuging: decide if this check needed, to say in forever? 
            // -- Is there at least 1 weight for every vertex
            for (size_t vertexIndex = 0; vertexIndex < destMesh.vertices.size(); vertexIndex++)
            {
                bool bThereIsAWeight = false;
                for (auto& vertexWeight : outVertexWeights) // check all weighs, check is there is a weight for the current vertex
                {
                    if (vertexWeight.vertexIndex == vertexIndex)
                    {
                        bThereIsAWeight = true;
                    }
                }

                if (!bThereIsAWeight)
                {
                    auto DEBUG_BREAK = 1;
                    LogActionError("Invalid Vertex Weights for mesh: " + destMesh.meshName + "a vertex has not weight");
                }
            }
        }

        static void ComputeTangentBasisForUnindexedMesh(
            // inputs
            const std::vector<sm::Vector3>& vertices,
            const std::vector<sm::Vector2>& uvs,
            const std::vector<sm::Vector3>& normals,
            // outputs
            std::vector<sm::Vector3>& tangents,
            std::vector<sm::Vector3>& bitangents)
        {
            for (unsigned int i = 0; i < vertices.size(); i += 3) {
                // Shortcuts for vertices
                const sm::Vector3& v0 = vertices[i + 0];
                const sm::Vector3& v1 = vertices[i + 1];
                const sm::Vector3& v2 = vertices[i + 2];

                // Shortcuts for UVs
                const sm::Vector2& uv0 = uvs[i + 0];
                const sm::Vector2& uv1 = uvs[i + 1];
                const sm::Vector2& uv2 = uvs[i + 2];

                // Edges of the triangle : postion delta
                sm::Vector3 deltaPos1 = v1 - v0;
                sm::Vector3 deltaPos2 = v2 - v0;

                // UV delta
                sm::Vector2 deltaUV1 = uv1 - uv0;
                sm::Vector2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
                sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
                sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

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
                const sm::Vector3& v0 = convert::ConvertToVec3(vertices[i + 0u].position);
                const sm::Vector3& v1 = convert::ConvertToVec3(vertices[i + 1u].position);
                const sm::Vector3& v2 = convert::ConvertToVec3(vertices[i + 2u].position);

                // Shortcuts for UVs
                const sm::Vector2& uv0 = vertices[i + 0u].uv;
                const sm::Vector2& uv1 = vertices[i + 1u].uv;
                const sm::Vector2& uv2 = vertices[i + 2u].uv;

                // Edges of the triangle : postion delta
                sm::Vector3 deltaPos1 = v1 - v0;
                sm::Vector3 deltaPos2 = v2 - v0;

                // UV delta
                sm::Vector2 deltaUV1 = uv1 - uv0;
                sm::Vector2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
                sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
                sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

                vertices[i + 0u].tangent = tangent;
                vertices[i + 1u].tangent = tangent;
                vertices[i + 2u].tangent = tangent;

                vertices[i + 0u].bitangent = bitangent;
                vertices[i + 1u].bitangent = bitangent;
                vertices[i + 2u].bitangent = bitangent;
            }
        };

        // TODO: FINISH
        static void ComputeTangentBasisIndexed(std::vector<PackedCommonVertex>& vertices, const std::vector<uint32_t>& indices)
        {
            // iterate over triangles
            for (size_t faceIndex = 0; faceIndex < indices.size(); faceIndex += 3)
            {
                // Corner index-to-vertices of triangle N
                const auto& cornerIndex0 = indices[faceIndex + 0U];
                const auto& cornerIndex1 = indices[faceIndex + 1U];
                const auto& cornerIndex2 = indices[faceIndex + 2U];

                const sm::Vector3& v0 = convert::ConvertToVec3(vertices[cornerIndex0].position);
                const sm::Vector3& v1 = convert::ConvertToVec3(vertices[cornerIndex1].position);
                const sm::Vector3& v2 = convert::ConvertToVec3(vertices[cornerIndex2].position);

                // Shortcuts for UVs
                const sm::Vector2& uv0 = vertices[cornerIndex0].uv;
                const sm::Vector2& uv1 = vertices[cornerIndex1].uv;
                const sm::Vector2& uv2 = vertices[cornerIndex2].uv;

                // Edges of the triangle : postion delta
                sm::Vector3 deltaPos1 = v1 - v0;
                sm::Vector3 deltaPos2 = v2 - v0;

                // UV delta
                sm::Vector2 deltaUV1 = uv1 - uv0;
                sm::Vector2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
                sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
                sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

                vertices[cornerIndex0].tangent = tangent;
                vertices[cornerIndex1].tangent = tangent;
                vertices[cornerIndex2].tangent = tangent;

                vertices[cornerIndex0].bitangent = bitangent;
                vertices[cornerIndex1].bitangent = bitangent;
                vertices[cornerIndex2].bitangent = bitangent;
            }
        }

        static inline void DoIndexingAndAverageTangents_Slow(
            const std::vector<sm::Vector3>& inVertices,
            const std::vector<sm::Vector2>& inUVs,
            const std::vector<sm::Vector3>& inNormals,
            const std::vector<sm::Vector3>& inTangents,
            const std::vector<sm::Vector3>& inBitangents,

            
            std::vector<uint32_t>& outIndices,
            std::vector<sm::Vector3>& outVertices,
            std::vector<sm::Vector2>& outUVs,
            std::vector<sm::Vector3>& outNormals,
            std::vector<sm::Vector3>& outTangents,
           std::vector<sm::Vector3>& outBitangents
        ) {
            //std::map<ExtPackedCommonVertex, unsigned short> VertexToOutIndex;

            std::vector<int> avg_count/*(inVertices.size(), 1)*/;
            // For each input vertex
            for (unsigned int i = 0; i < inVertices.size(); i++) {
                PackedCommonVertex packed;
                packed.position = convert::ConvertToVec3(inVertices[i]);
                packed.uv = inUVs[i];
                packed.normal = inNormals[i];

                // Try to find a similar vertex in out_XXXX
                uint32_t index;

                //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
                bool found = GetSimilarVertexIndex(inVertices[i], inUVs[i], inNormals[i], outVertices, outUVs, outNormals, index);

                if (found) { // A similar vertex is already in the VBO, use it instead !
                    outIndices.push_back(index);

                    // Average the tangents and the bitangents, for "smoothing"
                    outTangents[index] += inTangents[i];
                    outBitangents[index] += inBitangents[i];                    
                }
                else { // If not, it needs to be added in the output data.
                    outVertices.push_back(inVertices[i]);
                    outUVs.push_back(inUVs[i]);
                    outNormals.push_back(inNormals[i]);
                    outTangents.push_back(inTangents[i]);
                    outBitangents.push_back(inBitangents[i]);
                 
                    uint32_t newindex = (uint32_t)outVertices.size() - 1;

                    outIndices.push_back(newindex);                    
                }
            }
        }

        /// <summary>
        /// Makes an unindex mesh into an indexed one, by discarding indentical/very similar vertices.
        /// Makes an index buffer for the new index mesh
        /// Makes a "vertex remap", 
        /// newIndex =  remap[oldIndex], -1 for discarded vertices
        /// uses for vertex influence remapping,
        /// (could be use later, to split the DoIndexing..() method up into discrete methods)
        /// </summary>
        /// <param name="inVertices">Unindexed vertex input buffer</param>
        /// <param name="outVertices">Indexed vertex output buffer </param>
        /// <param name="outIndices">Index buffer</param>
        /// <param name="outVertexRemap">vertex remap</param>
        static inline void DoMeshIndexingWithTangenSmoothing_Slow(
            const std::vector<PackedCommonVertex>& inVertices,
            std::vector<PackedCommonVertex>& outVertices,
            std::vector<uint32_t>& outIndices,
            std::vector<int>& outVertexRemap)
        {
            outVertexRemap.clear(); // can never be too sure?:)        

            // For each input vertex
            for (unsigned int inVertexIndex = 0; inVertexIndex < inVertices.size(); inVertexIndex++) {

                // Try to find a similar vertex in out_XXXX
                uint32_t indexToMatchingVertex;

                bool matchingVertexFound = GetSimilarPackedVertexIndex_Slow(convert::ConvertToVec3(inVertices[inVertexIndex].position), inVertices[inVertexIndex].uv, inVertices[inVertexIndex].normal, outVertices, indexToMatchingVertex);

                if (matchingVertexFound) // A similar vertex is already in the new OUTPUT Vertex buffer, use that!
                {
                    outIndices.push_back(indexToMatchingVertex); // refer to existing vertex+vertexweight

                    // Average the tangents and the bitangents
                    outVertices[indexToMatchingVertex].tangent = sm::Vector3(outVertices[indexToMatchingVertex].tangent) + sm::Vector3(inVertices[inVertexIndex].tangent);
                    outVertices[indexToMatchingVertex].bitangent = sm::Vector3(outVertices[indexToMatchingVertex].bitangent) + sm::Vector3(inVertices[inVertexIndex].bitangent);

                    outVertexRemap.push_back(VERTEX_DISCARDED); // this a duplicate                    
                }
                else // No matching vertex found, add a vertex from the INPUT vertex buffer
                {
                    outVertices.push_back(inVertices[inVertexIndex]);

                    uint32_t newVertexIndex = (uint32_t)outVertices.size() - 1;
                    outIndices.push_back(newVertexIndex);

                    outVertexRemap.push_back(newVertexIndex);
                }
            }
        }

        static inline void DoMeshIndexingWithTangenSmoothing_OutPutRemap_Fast(
            const std::vector<PackedCommonVertex>& inVertices,
            std::vector<PackedCommonVertex>& outVertices,
            std::vector<uint32_t>& outIndices,
            std::vector<int>& outVertexRemap)
        {
            outVertexRemap.clear(); // can never be too sure?:)        
            std::map<PackedVertex, uint32_t> VertexToOutIndex;
            // For each input vertex
            for (unsigned int inVertexIndex = 0; inVertexIndex < inVertices.size(); inVertexIndex++) 
            {
                PackedVertex packedVertex;
                packedVertex.position = convert::ConvertToVec3(inVertices[inVertexIndex].position);
                packedVertex.uv = inVertices[inVertexIndex].uv;
                packedVertex.normal = inVertices[inVertexIndex].normal;
                
                uint32_t indexToMatchingVertex;                
                bool found = GetSimilarVertexIndex_Fast(packedVertex, VertexToOutIndex, indexToMatchingVertex);                

                bool matchingVertexFound = GetSimilarPackedVertexIndex_Slow(convert::ConvertToVec3(inVertices[inVertexIndex].position), inVertices[inVertexIndex].uv, inVertices[inVertexIndex].normal, outVertices, indexToMatchingVertex);

                if (matchingVertexFound) // A similar vertex is already in the new OUTPUT Vertex buffer, use that!
                {
                    outIndices.push_back(indexToMatchingVertex); // refer to existing vertex+vertexweight

                    // Average the tangents and the bitangents
                    outVertices[indexToMatchingVertex].tangent = sm::Vector3(outVertices[indexToMatchingVertex].tangent) + sm::Vector3(inVertices[inVertexIndex].tangent);
                    outVertices[indexToMatchingVertex].bitangent = sm::Vector3(outVertices[indexToMatchingVertex].bitangent) + sm::Vector3(inVertices[inVertexIndex].bitangent);

                    outVertexRemap.push_back(VERTEX_DISCARDED); // this a duplicate                    
                }
                else // No matching vertex found, add a vertex from the INPUT vertex buffer
                {
                    outVertices.push_back(inVertices[inVertexIndex]);

                    uint32_t newVertexIndex = (uint32_t)outVertices.size() - 1;
                    outIndices.push_back(newVertexIndex);

                    outVertexRemap.push_back(newVertexIndex);
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

        static bool GetSimilarVertexIndex_Fast(
            PackedVertex& packed,
            std::map<PackedVertex, uint32_t>& VertexToOutIndex,
            uint32_t& result
        ) {
            std::map<PackedVertex, uint32_t>::iterator it = VertexToOutIndex.find(packed);
            if (it == VertexToOutIndex.end()) {
                return false;
            }
            else {
                result = it->second;
                return true;
            }
        }


        static inline void indexVBO_TBN_Fast_Packed(
            const std::vector<PackedCommonVertex>& inVertices,
            std::vector<uint32_t>& out_indices,
            std::vector<PackedCommonVertex>& out_vertices
        ) {
            std::map<PackedVertex, uint32_t> VertexToOutIndex;

            // For each input vertex
            for (unsigned int i = 0; i < inVertices.size(); i++)
            {
                PackedVertex packedVertex;
                packedVertex.position = convert::ConvertToVec3(inVertices[i].position);
                packedVertex.uv = inVertices[i].uv;
                packedVertex.normal = inVertices[i].normal;

                // Try to find a similar vertex in out_XXXX
                uint32_t index;

                //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
                //bool found = getSimilarVertexIndex(packed, VertexToOutIndex, index);
                bool found = GetSimilarVertexIndex_Fast(packedVertex, VertexToOutIndex, index);

                if (found) { // A similar vertex is already in the VBO, use it instead !
                    out_indices.push_back(index);

                    // Average the tangents and the bitangents
                    out_vertices[index].tangent = sm::Vector3(out_vertices[index].tangent) + sm::Vector3(inVertices[i].tangent);
                    out_vertices[index].bitangent = sm::Vector3(out_vertices[index].bitangent) + sm::Vector3(inVertices[i].bitangent);
                }
                else
                { // If not, it needs to be added in the output data.
                    out_vertices.push_back(inVertices[i]);

                    uint32_t newindex = (uint32_t)out_vertices.size() - 1;

                    out_indices.push_back(newindex);
                    VertexToOutIndex[packedVertex] = newindex;
                }
            }
        }

        /// <summary>
        /// Compare two floats within some tolerance
        /// Used for "slow vertex search"
        /// </summary>        
        static inline bool IsNear(float v1, float v2) {
            return fabs(v1 - v2) < 0.00001f;
        }

        /// <summary>
        /// Searches through all the so-far picked vertices
        /// for a similar vertex 
        /// Similar = similar position && similar UVs && similar normal
        /// within some abritary tolerance
        /// </summary>        
        static inline bool GetSimilarVertexIndex(
            const sm::Vector3& in_vertex,
            const sm::Vector2& in_uv,
            const sm::Vector3& in_normal,
            std::vector<sm::Vector3>& out_vertices,
            std::vector<sm::Vector2>& out_uvs,
            std::vector<sm::Vector3>& out_normals,
            uint32_t& result
        ) {
            // Lame linear search
            for (unsigned int i = 0; i < out_vertices.size(); i++) {
                if (
                    IsNear(in_vertex.x, out_vertices[i].x) &&
                    IsNear(in_vertex.y, out_vertices[i].y) &&
                    IsNear(in_vertex.z, out_vertices[i].z) &&
                    IsNear(in_uv.x, out_uvs[i].x) &&
                    IsNear(in_uv.y, out_uvs[i].y) &&
                    IsNear(in_normal.x, out_normals[i].x) &&
                    IsNear(in_normal.y, out_normals[i].y) &&
                    IsNear(in_normal.z, out_normals[i].z)
                    ) {
                    result = i;
                    return true;
                }
            }
            // No other vertex could be used instead.
            // Looks like we'll have to add it to the VBO.
            return false;
        }

        /// <summary>
        /// A lot of code/help from http://www.opengl-tutorial.org/download/
        /// Searches through all the so-far picked vertices
        /// for a similar vertex 
        /// Similar = similar position && similar UVs && similar normal
        /// within some abritary tolerance
        /// </summary>        
        static inline bool GetSimilarPackedVertexIndex_Slow(
            const sm::Vector3& in_vertex,
            const sm::Vector2& in_uv,
            const sm::Vector3& in_normal,
            std::vector<PackedCommonVertex>& out_vertices,

            uint32_t& result
        ) {
            // Lame linear search
            for (unsigned int i = 0; i < out_vertices.size(); i++) {
                if (
                    IsNear(in_vertex.x, out_vertices[i].position.x) &&
                    IsNear(in_vertex.y, out_vertices[i].position.y) &&
                    IsNear(in_vertex.z, out_vertices[i].position.z) &&
                    IsNear(in_uv.x, out_vertices[i].uv.x) &&
                    IsNear(in_uv.y, out_vertices[i].uv.y) &&
                    IsNear(in_normal.x, out_vertices[i].normal.x) &&
                    IsNear(in_normal.y, out_vertices[i].normal.y) &&
                    IsNear(in_normal.z, out_vertices[i].normal.z)
                    ) {
                    result = i;
                    return true;
                }
            }
            // No other vertex could be used instead.
            // Looks like we'll have to add it to the VBO.
            return false;
        }


        /// <summary>
        /// For use in the "EVEN-faster simiar vertex search", which is not writting yet
        /// </summary>
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
            //PackedVertexExt
            //(sm::Vector3 position,
            //    sm::Vector2 uv,
            //    sm::Vector3 normal)



            //{

            //    using namespace DirectX;
            //    posX = DirectX::PackedVector::XMConvertFloatToHalf(position.x);
            //    posY = DirectX::PackedVector::XMConvertFloatToHalf(position.y);
            //    posZ = DirectX::PackedVector::XMConvertFloatToHalf(position.z);

            //    uvX = DirectX::PackedVector::XMConvertFloatToHalf(uv.x);
            //    uvY = DirectX::PackedVector::XMConvertFloatToHalf(uv.y);

            //    // TODO: finsih? or throw out?


            //}
            //bool operator<(const PackedVertex that) const
            //{
            //    return memcmp((void*)this, (void*)&that, sizeof(PackedVertex)) > 0;
            //};
        
        
        };
    };
};