#pragma once

namespace wrapdll
{
	/// <summary>
	/// Base class for interop objects, 
	/// Only function atm, is to simplify clean-up of pointer used in C# layer
	/// </summary>
	class BaseInteropObject
	{
	public:
		virtual ~BaseInteropObject()
		{
		}
	};
}
