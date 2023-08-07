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
#include <chrono>
#include <codecvt>

/// <summary>
/// Overloads std::tolower()/toupper() to std::string (only works single char in STL)
/// </summary>
namespace std
{
	static std::string tolower(const std::string& _strInput)
	{
		std::string strOut = _strInput;

		std::transform(strOut.begin(), strOut.end(), strOut.begin(), ::tolower);

		return strOut;
	}

	static std::string toupper(const std::string& _strInput)
	{
		std::string strOut = _strInput;

		std::transform(strOut.begin(), strOut.end(), strOut.begin(), ::tolower);

		return strOut;
	}
}

static std::wstring WidenStr(const std::string& str)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.from_bytes(str);
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
			if (tolower(items[itemIndex]) == tolower(value))
				return itemIndex;
		}

		return -1;
	}

	class SystemClock
	{
	public:
		double GetSeconds()
		{
			typedef std::chrono::high_resolution_clock Time;
			typedef std::chrono::duration<float> fsec;
			
			double ticks = static_cast<double>(std::chrono::high_resolution_clock::now().time_since_epoch().count());
			double period = std::chrono::high_resolution_clock::period::den;

			double seconds = ticks / period;			

			return seconds;			
		}

		double GetLocalTime() 
		{
			double timeElapsed = GetSeconds() - m_startTime; // +m_start_at;
			return timeElapsed;
		}

	private:
		double m_startTime = 0;
	};

}
