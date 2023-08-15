#pragma once

#include <DirectXMath.h>
#include <vector>
#include <string>

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
        strcpy_s<255>(this->boneName, boneName.c_str());
        this->boneIndex = boneIndex;
        this->weight = weight;
    }

    VertexInfluence Clone() const
    {
        VertexInfluence v;
        strcpy_s<255>(v.boneName, boneName);
        v.boneIndex = boneIndex;
        v.weight = weight;

        return v;
    }    

    char boneName[255] = "";
	uint32_t boneIndex = 0;
	float weight = 0.0f;
};

struct VertexWeight
{    
    char boneName[255] = "";
    int vertexIndex = 0;
    float vertexWeight = 0.0f;
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

    //PackedCommonVertex() {};
    //PackedCommonVertex(PackedCommonVertex& v)
    //{
    //    *this = v;
    //}

	DirectX::XMFLOAT4 position = { 0, 0, 0, 0 };
	DirectX::XMFLOAT3 normal = { 0, 0, 0 };
	DirectX::XMFLOAT3 bitangent = { 0, 0, 0 };
	DirectX::XMFLOAT3 tangent = { 0, 0, 0 };
	DirectX::XMFLOAT2 uv = { 0, 0 };
	DirectX::XMFLOAT4 color = { 1, 0, 0, 1 };
	/*VertexInfluence influences[4];*/
	int weightCount = 0;
};


struct PackedMesh
{
	std::string meshName = "Unnamed_Mesh\0";
	std::vector<PackedCommonVertex> vertices;
	std::vector<uint16_t> indices;
	std::vector<VertexWeight> vertexWeights;
};

