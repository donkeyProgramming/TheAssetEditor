/*
 * tools.h
 *
 * Various tool functions
 *
 * phazer, 2023 
 *
 */
#pragma once

#include <vector>
#include <string>
#include <algorithm>

/// <summary>
/// Overloads std::tolower()/toupper() to std::string (only works single char in STL)
/// </summary>
namespace std
{
	static string tolower(const std::string& _strInput)
	{
		std::string strOut = _strInput;

		std::transform(strOut.begin(), strOut.end(), strOut.begin(), ::tolower);

		return strOut;
	}

	static string toupper(const std::string& _strInput)
	{
		std::string strOut = _strInput;

		std::transform(strOut.begin(), strOut.end(), strOut.begin(), ::tolower);

		return strOut;
	}
}

/// <summary>
/// Various misc functions
/// </summary>
namespace tools
{
	/*template <typename T>
	static int GetIndexOf(const T& value, const std::vector<T>& items)
	{
		for (int itemIndex = 0; itemIndex < items.size(); itemIndex++)
		{
			if (items[itemIndex] == value)
				return itemIndex;
		}
	}

	template<>*/
	static int GetIndexOf(const std::string& value, const std::vector<std::string>& items)
	{
		for (int itemIndex = 0; itemIndex < items.size(); itemIndex++)
		{
			if (std::tolower(items[itemIndex]) == std::tolower(value))
				return itemIndex;
		}

		return -1;
	}
}
