#pragma once

#include <SimpleMath.h>

namespace convert
{
	static sm::Vector3 ConvertToVec3(const sm::Vector4& in)
	{
		return { in.x, in.y, in.z };
	}

	static sm::Vector4 ConvertToVect4(const sm::Vector3& in)
	{
		return { in.x, in.y, in.z, 0 };
	}
}