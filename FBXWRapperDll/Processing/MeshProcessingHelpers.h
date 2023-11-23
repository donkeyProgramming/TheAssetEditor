#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "../HelperUtils/VectorConverter.h"

class MeshProcessingHelpers
{
    static void  CalculateRawTangents(PackedCommonVertex& v)
    {
        const sm::Vector3& v0 = convert::ConvertToVec3(v.position);
        const sm::Vector3& v1 = convert::ConvertToVec3(v.position);
        const sm::Vector3& v2 = convert::ConvertToVec3(v.position);

        // Shortcuts for UVs
        const sm::Vector2& uv0 = v.uv;
        const sm::Vector2& uv1 = v.uv;
        const sm::Vector2& uv2 = v.uv;

        // Edges of the triangle : postion delta
        sm::Vector3 deltaPos1 = v1 - v0;
        sm::Vector3 deltaPos2 = v2 - v0;

        // UV delta
        sm::Vector2 deltaUV1 = uv1 - uv0;
        sm::Vector2 deltaUV2 = uv2 - uv0;

        float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
        sm::Vector3 tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
        sm::Vector3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

        v.tangent = tangent;
        v.tangent = tangent;
        v.tangent = tangent;

        v.bitangent = bitangent;
        v.bitangent = bitangent;
        v.bitangent = bitangent;
    }

};

