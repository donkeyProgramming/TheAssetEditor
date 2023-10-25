#pragma once

#include <DirectXMath.h>
#include <vector>
#include <string>
#include "FileInfoData.h"

struct BoneInfo
{
    int id;
    int parentId;
    FixedString name;
    
    DirectX::XMFLOAT4 localRotation;
    DirectX::XMFLOAT3 localTranslation;
};

struct BoneAnimKey
{
	DirectX::XMFLOAT3 translation = { 0,0,0 };
	DirectX::XMFLOAT4 quaternion = { 0,0,0,1 };
	double timeStampe = 0.0;
};

struct VertexInfluence
{
public:
    VertexInfluence() {};

    VertexInfluence(const VertexInfluence& v)
    {
        *this = v.Clone();
    }

    void Set(const std::string& boneName, uint32_t boneIndex, float Weight)
    {        
        CopyToFixedString(this->boneName, boneName);

        this->boneIndex = boneIndex;
        this->weight = weight;
    }

    VertexInfluence Clone() const
    {
        VertexInfluence v;
        CopyFixedString(v.boneName, boneName);
        v.boneIndex = boneIndex;
        v.weight = weight;

        return v;
    }    

    FixedString boneName = ""; // fixed length for simpler interop
	uint32_t boneIndex = 0;
	float weight = 0.0f;
};

struct VertexWeight
{    
    char boneName[256] = "";    
    int boneIndex; // TODO: should be removed, maybe, as it is not known when struct is first filled
    int vertexIndex = 0;
    float weight = 0.0f;
};




//struct VertexInfluenceExt
//{
//	std::string boneName = "";
//	uint32_t boneIndex = 0;
//	float weight = 0.0f;
//};
//

struct ControlPointInfluence
{	
	VertexInfluence influences[4];
	int weightCount = 0;
};

struct PackedCommonVertex
{
	DirectX::XMFLOAT4 position = { 0, 0, 0, 0 };
	DirectX::XMFLOAT3 normal = { 0, 0, 0 };
	DirectX::XMFLOAT3 bitangent = { 0, 0, 0 };
	DirectX::XMFLOAT3 tangent = { 0, 0, 0 };
	DirectX::XMFLOAT2 uv = { 0, 0 };
	DirectX::XMFLOAT4 color = { 1, 0, 0, 1 };
};


struct PackedMesh
{
	std::string meshName = "Unnamed_Mesh\0";
	std::vector<PackedCommonVertex> vertices;
	std::vector<uint32_t> indices;
	std::vector<VertexWeight> vertexWeights;
};

